using UnityEngine;

namespace ARMonster.Core
{
    /// <summary>
    /// Поворачивает объект так, чтобы он всегда был направлен к главной камере только по оси Y,
    /// предотвращая наклон вверх/вниз и сохраняя вертикальную ориентацию.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                    return;
            }

            Vector3 directionToCamera = _mainCamera.transform.position - transform.position;
            directionToCamera.y = 0;

            if (directionToCamera.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }
    }
}
