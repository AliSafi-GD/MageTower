using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Keeps the side-view layout anchored to the screen edges: tower (and mage) at the left edge,
    /// spawn point off the right edge, ground spanning the width. Recomputes only when the screen size changes.
    /// </summary>
    public sealed class CombatLayout : MonoBehaviour
    {
        private CombatConfig _config;
        private Camera _camera;
        private Transform _ground;
        private Transform _towerTransform;
        private Tower _tower;
        private Transform _mage;
        private EnemySpawner _spawner;
        private int _lastWidth = -1;
        private int _lastHeight = -1;

        /// <summary>Injects the objects to lay out and applies the layout immediately.</summary>
        public void Initialize(CombatConfig config, Camera camera, Transform ground, Tower tower, Transform mage, EnemySpawner spawner)
        {
            _config = config;
            _camera = camera;
            _ground = ground;
            _tower = tower;
            _towerTransform = tower.transform;
            _mage = mage;
            _spawner = spawner;
            Apply();
        }

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight) Apply();
        }

        private void Apply()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            if (_camera == null) return;

            float halfWidth = _config.CameraOrthoSize * _camera.aspect;
            float towerX = -halfWidth + _config.TowerMarginFromScreenEdge;

            float groundThickness = _config.CameraOrthoSize;
            _ground.position = new Vector3(0f, _config.GroundY - groundThickness * 0.5f, 0f);
            _ground.localScale = new Vector3(halfWidth * 2f + 4f, groundThickness, 1f);

            _towerTransform.position = new Vector3(towerX, _config.GroundY + _config.TowerHeight * 0.5f, 0f);
            _tower.SetContactX(towerX + _config.TowerWidth * 0.5f);

            Vector3 magePos = _mage.position;
            magePos.x = towerX;
            _mage.position = magePos;

            _spawner.SetSpawnX(halfWidth + _config.SpawnMarginOffScreen);
        }
    }
}
