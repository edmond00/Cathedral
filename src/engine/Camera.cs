using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cathedral.Engine
{
    public class Camera
    {
        // Camera state
        private float _yaw = 0f;
        private float _pitch = 0f;
        private float _distance = 80.0f; // Default distance
        
        // Debug camera system
        private bool _debugCameraMode = false;
        private int _debugCameraAngle = 0; // 0=side view, 1=top view, 2=front view, 3=diagonal
        private float _debugCameraDistance = 120.0f;
        
        // Camera limits and speeds
        private const float ROTATION_SPEED = 60f;
        private const float ZOOM_SPEED = 15.0f;
        private const float MIN_DISTANCE = 0.1f;
        private const float MAX_DEBUG_DISTANCE = 300.0f;
        private const float MIN_DEBUG_DISTANCE = 50.0f;
        private const float MIN_PITCH = -85f;
        private const float MAX_PITCH = 85f;
        
        // Events for camera state changes
        public event Action<bool>? DebugModeChanged;
        public event Action<float, float, float>? CameraTransformed; // yaw, pitch, distance
        
        // Properties for external access
        public float Yaw => _yaw;
        public float Pitch => _pitch;
        public float Distance => _distance;
        public bool IsDebugMode => _debugCameraMode;
        public int DebugAngle => _debugCameraAngle;
        public float DebugDistance => _debugCameraDistance;
        
        /// <summary>
        /// Gets the current view matrix for rendering
        /// </summary>
        public Matrix4 GetViewMatrix()
        {
            if (_debugCameraMode)
            {
                return GetDebugCameraMatrix();
            }
            
            return GetMainCameraMatrix();
        }
        
        /// <summary>
        /// Gets the main camera view matrix (orbital camera around origin)
        /// </summary>
        private Matrix4 GetMainCameraMatrix()
        {
            float yawR = MathHelper.DegreesToRadians(_yaw);
            float pitchR = MathHelper.DegreesToRadians(_pitch);
            Vector3 camDir = new Vector3(
                MathF.Cos(pitchR) * MathF.Cos(yawR),
                MathF.Sin(pitchR),
                MathF.Cos(pitchR) * MathF.Sin(yawR)
            );
            Vector3 camPos = -camDir * _distance;
            return Matrix4.LookAt(camPos, Vector3.Zero, Vector3.UnitY);
        }
        
        /// <summary>
        /// Gets the debug camera view matrix (fixed angle views)
        /// </summary>
        private Matrix4 GetDebugCameraMatrix()
        {
            Vector3 debugCamPos;
            Vector3 upVector;
            
            switch (_debugCameraAngle)
            {
                case 0: // Side view (X-axis)
                    debugCamPos = new Vector3(_debugCameraDistance, 0, 0);
                    upVector = Vector3.UnitY;
                    break;
                case 1: // Top view (Y-axis)
                    debugCamPos = new Vector3(0, _debugCameraDistance, 0);
                    upVector = Vector3.UnitZ;
                    break;
                case 2: // Front view (Z-axis)
                    debugCamPos = new Vector3(0, 0, _debugCameraDistance);
                    upVector = Vector3.UnitY;
                    break;
                case 3: // Diagonal view
                    debugCamPos = new Vector3(
                        _debugCameraDistance * 0.7f, 
                        _debugCameraDistance * 0.5f, 
                        _debugCameraDistance * 0.7f);
                    upVector = Vector3.UnitY;
                    break;
                default:
                    debugCamPos = new Vector3(_debugCameraDistance, 0, 0);
                    upVector = Vector3.UnitY;
                    break;
            }
            
            return Matrix4.LookAt(debugCamPos, Vector3.Zero, upVector);
        }
        
        /// <summary>
        /// Gets the current camera position in world space
        /// </summary>
        public Vector3 GetCameraPosition()
        {
            if (_debugCameraMode)
            {
                return GetDebugCameraPosition();
            }
            
            float yawR = MathHelper.DegreesToRadians(_yaw);
            float pitchR = MathHelper.DegreesToRadians(_pitch);
            Vector3 camDir = new Vector3(
                MathF.Cos(pitchR) * MathF.Cos(yawR),
                MathF.Sin(pitchR),
                MathF.Cos(pitchR) * MathF.Sin(yawR)
            );
            return -camDir * _distance;
        }
        
        /// <summary>
        /// Gets the debug camera position in world space
        /// </summary>
        private Vector3 GetDebugCameraPosition()
        {
            switch (_debugCameraAngle)
            {
                case 0: return new Vector3(_debugCameraDistance, 0, 0);
                case 1: return new Vector3(0, _debugCameraDistance, 0);
                case 2: return new Vector3(0, 0, _debugCameraDistance);
                case 3: return new Vector3(
                    _debugCameraDistance * 0.7f, 
                    _debugCameraDistance * 0.5f, 
                    _debugCameraDistance * 0.7f);
                default: return new Vector3(_debugCameraDistance, 0, 0);
            }
        }
        
        /// <summary>
        /// Gets the camera direction vector (normalized)
        /// </summary>
        public Vector3 GetCameraDirection()
        {
            if (_debugCameraMode)
            {
                return Vector3.Normalize(Vector3.Zero - GetDebugCameraPosition());
            }
            
            float yawR = MathHelper.DegreesToRadians(_yaw);
            float pitchR = MathHelper.DegreesToRadians(_pitch);
            return new Vector3(
                MathF.Cos(pitchR) * MathF.Cos(yawR),
                MathF.Sin(pitchR),
                MathF.Cos(pitchR) * MathF.Sin(yawR)
            );
        }
        
        /// <summary>
        /// Handles camera input. Returns true if any camera controls were processed.
        /// </summary>
        public bool ProcessInput(KeyboardState keyboardState, FrameEventArgs args)
        {
            bool inputProcessed = false;
            
            // Rotation controls (arrow keys)
            if (keyboardState.IsKeyDown(Keys.Left))
            {
                _yaw -= ROTATION_SPEED * (float)args.Time;
                inputProcessed = true;
            }
            if (keyboardState.IsKeyDown(Keys.Right))
            {
                _yaw += ROTATION_SPEED * (float)args.Time;
                inputProcessed = true;
            }
            if (keyboardState.IsKeyDown(Keys.Up))
            {
                _pitch = Math.Clamp(_pitch + ROTATION_SPEED * (float)args.Time, MIN_PITCH, MAX_PITCH);
                inputProcessed = true;
            }
            if (keyboardState.IsKeyDown(Keys.Down))
            {
                _pitch = Math.Clamp(_pitch - ROTATION_SPEED * (float)args.Time, MIN_PITCH, MAX_PITCH);
                inputProcessed = true;
            }
            
            // Zoom/Distance controls (W/S keys)
            if (keyboardState.IsKeyDown(Keys.W))
            {
                if (_debugCameraMode)
                {
                    _debugCameraDistance = MathF.Max(MIN_DEBUG_DISTANCE, 
                        _debugCameraDistance - ZOOM_SPEED * (float)args.Time);
                }
                else
                {
                    _distance = MathF.Max(MIN_DISTANCE, _distance - ZOOM_SPEED * (float)args.Time);
                }
                inputProcessed = true;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                if (_debugCameraMode)
                {
                    _debugCameraDistance = MathF.Min(MAX_DEBUG_DISTANCE, 
                        _debugCameraDistance + ZOOM_SPEED * (float)args.Time);
                }
                else
                {
                    _distance += ZOOM_SPEED * (float)args.Time;
                }
                inputProcessed = true;
            }
            
            // Debug camera toggle (C key)
            if (keyboardState.IsKeyPressed(Keys.C))
            {
                ToggleDebugMode();
                inputProcessed = true;
            }
            
            // Debug camera angle switching (V key)
            if (_debugCameraMode && keyboardState.IsKeyPressed(Keys.V))
            {
                CycleDebugAngle();
                inputProcessed = true;
            }
            
            // Fire transformation event if camera moved
            if (inputProcessed)
            {
                CameraTransformed?.Invoke(_yaw, _pitch, _distance);
            }
            
            return inputProcessed;
        }
        
        /// <summary>
        /// Toggles between main camera and debug camera modes
        /// </summary>
        public void ToggleDebugMode()
        {
            _debugCameraMode = !_debugCameraMode;
            string cameraType = _debugCameraMode ? "Debug camera" : "Main camera";
            string controls = _debugCameraMode ? 
                " (Use W/S to move closer/farther, V to change angle)" : 
                " (Use W/S to zoom, arrows to rotate)";
            Console.WriteLine($"Camera switched to: {cameraType}{controls}");
            
            DebugModeChanged?.Invoke(_debugCameraMode);
        }
        
        /// <summary>
        /// Cycles through debug camera angles
        /// </summary>
        public void CycleDebugAngle()
        {
            if (!_debugCameraMode) return;
            
            _debugCameraAngle = (_debugCameraAngle + 1) % 4;
            string angleDesc = _debugCameraAngle switch
            {
                0 => "Side view (X-axis)",
                1 => "Top view (Y-axis)",
                2 => "Front view (Z-axis)",
                3 => "Diagonal view",
                _ => "Unknown"
            };
            Console.WriteLine($"Debug camera angle: {angleDesc}");
        }
        
        /// <summary>
        /// Centers the camera to look at a specific world position from the appropriate angle
        /// </summary>
        /// <param name="worldPosition">The position to focus on</param>
        /// <param name="maintainDistance">Whether to keep current distance or auto-adjust</param>
        public void FocusOnPosition(Vector3 worldPosition, bool maintainDistance = true)
        {
            if (_debugCameraMode)
            {
                // In debug mode, we can't easily "focus" on arbitrary positions
                // So we switch back to main camera mode
                _debugCameraMode = false;
                DebugModeChanged?.Invoke(false);
            }
            
            // Store the current camera distance if requested
            float originalDistance = maintainDistance ? _distance : _distance;
            
            // Calculate the direction from origin to target position
            Vector3 fromOriginToTarget = worldPosition.Normalized();
            
            // Position camera along the line from origin through target, at desired distance
            Vector3 desiredCameraPos = fromOriginToTarget * originalDistance;
            
            // Calculate camera angles to look toward origin from this position
            Vector3 camDir = -desiredCameraPos.Normalized();
            
            // Update camera parameters
            _distance = originalDistance;
            _yaw = MathHelper.RadiansToDegrees(MathF.Atan2(camDir.Z, camDir.X));
            _pitch = MathHelper.RadiansToDegrees(MathF.Asin(camDir.Y));
            
            // Clamp pitch to avoid gimbal lock near poles
            _pitch = Math.Clamp(_pitch, MIN_PITCH, MAX_PITCH);
            
            CameraTransformed?.Invoke(_yaw, _pitch, _distance);
        }
        
        /// <summary>
        /// Creates a projection matrix with the specified parameters
        /// </summary>
        public Matrix4 CreateProjectionMatrix(int windowWidth, int windowHeight, float fovY = 60f, float nearPlane = 0.01f, float farPlane = 1000f)
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(fovY),
                (float)windowWidth / windowHeight,
                nearPlane,
                farPlane);
        }
        
        /// <summary>
        /// Calculates a mouse ray from screen coordinates
        /// </summary>
        public (Vector3 rayOrigin, Vector3 rayDirection) GetMouseRay(Vector2 mousePos, int windowWidth, int windowHeight, float fovY = 60f)
        {
            // Convert mouse coordinates to normalized device coordinates
            float x = (2.0f * mousePos.X) / windowWidth - 1.0f;
            float y = 1.0f - (2.0f * mousePos.Y) / windowHeight;
            
            // Get camera properties
            Vector3 rayOrigin = GetCameraPosition();
            Vector3 camDir = GetCameraDirection();
            
            // Calculate camera basis vectors
            Vector3 up = Vector3.UnitY;
            Vector3 right = Vector3.Normalize(Vector3.Cross(camDir, up));
            Vector3 cameraUp = Vector3.Cross(right, camDir);
            
            // Calculate ray direction
            float fovYRad = MathHelper.DegreesToRadians(fovY);
            float aspect = (float)windowWidth / windowHeight;
            
            float tanHalfFov = MathF.Tan(fovYRad / 2.0f);
            Vector3 rayDirection = Vector3.Normalize(
                camDir +
                (right * x * tanHalfFov * aspect) +
                (cameraUp * y * tanHalfFov)
            );
            
            return (rayOrigin, rayDirection);
        }
        
        /// <summary>
        /// Sets camera parameters directly (for external control)
        /// </summary>
        public void SetCameraParameters(float yaw, float pitch, float distance)
        {
            _yaw = yaw;
            _pitch = Math.Clamp(pitch, MIN_PITCH, MAX_PITCH);
            _distance = MathF.Max(MIN_DISTANCE, distance);
            
            CameraTransformed?.Invoke(_yaw, _pitch, _distance);
        }
        
        /// <summary>
        /// Sets debug camera parameters directly
        /// </summary>
        public void SetDebugCameraParameters(bool debugMode, int angle = 0, float distance = 120.0f)
        {
            bool modeChanged = _debugCameraMode != debugMode;
            _debugCameraMode = debugMode;
            _debugCameraAngle = Math.Clamp(angle, 0, 3);
            _debugCameraDistance = Math.Clamp(distance, MIN_DEBUG_DISTANCE, MAX_DEBUG_DISTANCE);
            
            if (modeChanged)
            {
                DebugModeChanged?.Invoke(_debugCameraMode);
            }
        }
        
        /// <summary>
        /// Resets camera to default state
        /// </summary>
        public void Reset()
        {
            _yaw = 0f;
            _pitch = 0f;
            _distance = 80.0f;
            _debugCameraMode = false;
            _debugCameraAngle = 0;
            _debugCameraDistance = 120.0f;
            
            DebugModeChanged?.Invoke(false);
            CameraTransformed?.Invoke(_yaw, _pitch, _distance);
        }
    }
}