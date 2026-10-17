using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Wave-driven spawner (design/game-brief.md MVP feature 2): spawns each wave's enemies at a fixed spacing,
    /// then waits for the wave to be cleared plus a configured delay before starting the next. Waves never end.
    /// The boss of a boss wave is the last enemy spawned.
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        /// <summary>Raised when any spawned enemy is hit: (enemy, damage, wasCrit).</summary>
        public event Action<Enemy, float, bool> OnEnemyDamaged;
        /// <summary>Raised when an enemy is killed by damage (never for enemies that reach the tower).</summary>
        public event Action<Enemy> OnEnemyKilled;

        private readonly List<Enemy> _enemies = new List<Enemy>();
        private CombatConfig _config;
        private Tower _tower;
        private WaveModel _waves;
        private Sprite _sprite;
        private Color _color;
        private float _spawnX;
        private float _spawnTimer;
        private float _delayTimer;
        private int _remainingToSpawn;
        private bool _bossPending;

        /// <summary>Live enemies (shared by reference with the mage).</summary>
        public IReadOnlyList<Enemy> Enemies => _enemies;

        /// <summary>Injects dependencies.</summary>
        public void Initialize(CombatConfig config, Tower tower, WaveModel waves, Sprite sprite, Color color, float spawnX)
        {
            _config = config;
            _tower = tower;
            _waves = waves;
            _sprite = sprite;
            _color = color;
            _spawnX = spawnX;
            StartWave(); // wave 1 starts immediately, so the HUD never shows wave 0
        }

        /// <summary>Updates the off-screen spawn X after a layout change (affects future spawns only).</summary>
        public void SetSpawnX(float spawnX) => _spawnX = spawnX;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_remainingToSpawn > 0)
            {
                _spawnTimer -= dt;
                if (_spawnTimer <= 0f)
                {
                    // Clamp so a long frame spawns at most one enemy (no backlog burst).
                    _spawnTimer = Mathf.Max(_spawnTimer + _config.EnemySpawnInterval, 0f);
                    _remainingToSpawn--;
                    Spawn(_bossPending && _remainingToSpawn == 0);
                }
                return;
            }

            if (_enemies.Count > 0) return; // wave not cleared yet

            _delayTimer -= dt;
            if (_delayTimer > 0f) return;
            StartWave();
        }

        private void StartWave()
        {
            _waves.StartNextWave();
            int wave = _waves.CurrentWave;
            _remainingToSpawn = WaveScaling.EnemyCount(_config, wave);
            _bossPending = WaveScaling.IsBossWave(_config, wave);
            _spawnTimer = 0f; // first enemy immediately
            _delayTimer = _config.InterWaveDelay;
        }

        private void Spawn(bool boss)
        {
            int wave = _waves.CurrentWave;
            float size = _config.EnemySize * (boss ? _config.BossSizeMultiplier : 1f);
            float hp = boss ? WaveScaling.BossHp(_config, wave) : WaveScaling.EnemyHp(_config, wave);

            var go = new GameObject(boss ? "Boss" : "Enemy");
            go.transform.position = new Vector3(_spawnX, _config.GroundY + size * 0.5f, 0f);
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = boss ? _config.BossColor : _color;
            sr.sortingOrder = boss ? 6 : 5;
            var enemy = go.AddComponent<Enemy>();
            enemy.Initialize(_config, _tower, hp, WaveScaling.EnemyContactDamage(_config, wave), size, boss);
            enemy.GoldReward = WaveScaling.KillGold(_config, wave, boss);
            enemy.OnDamaged += HandleDamaged;
            enemy.OnKilled += HandleKilled;
            enemy.OnRemoved += HandleRemoved;
            _enemies.Add(enemy);
        }

        private void HandleDamaged(Enemy enemy, float damage, bool crit) => OnEnemyDamaged?.Invoke(enemy, damage, crit);

        private void HandleKilled(Enemy enemy) => OnEnemyKilled?.Invoke(enemy);

        private void HandleRemoved(Enemy enemy) => _enemies.Remove(enemy);
    }
}
