using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FinalProject
{
    public class Spring
    {
        public PhysicsBody A;
        public PhysicsBody B;
        public float RestLength;
    }

    public class SpringManager : MonoBehaviour
    {
        private const float INTERACT_RADIUS = 0.5f;

        private List<Spring> _springs = new();
        private Dictionary<PhysicsBody, List<PhysicsBody>> _network = new();

        [SerializeField] private float _snapLength;
        [SerializeField] private SpringSettings _settings;

        private void FixedUpdate()
        {
            List<Spring> springsToRemove = new();
            foreach (var spring in _springs)
            {
                
                // Snapping
                if (Vector3.Distance(spring.A.transform.position, spring.B.transform.position) > _snapLength)
                {
                    springsToRemove.Add(spring);
                }
                else
                {
                    // Spring is NOT stretched to far, apply forces
                    
                    var otherForce = ComputeForce(spring.A, spring.B, spring.RestLength, _settings);
                    spring.A.AddForce(-otherForce);
                    spring.B.AddForce(otherForce);
                }
                

                // Interactivity
                if (Input.GetMouseButton((int)MouseButton.RightMouse))
                {
                    var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorldPos.z = 0;

                    var a = spring.A.transform.position;
                    var b = spring.B.transform.position;

                    var distance = Vector3.Distance(a, b);
                    var direction = (b - a).normalized;
                    var aDot = Vector3.Dot(direction, a);
                    var bDot = Vector3.Dot(direction, b);
                    var lengthAlong = Vector3.Dot(direction, mouseWorldPos);
                    var closestPoint = Vector3.Lerp(a, b, (Mathf.Clamp(lengthAlong, aDot, bDot) - aDot) / distance);

                    // Debug.Log(Vector3.Distance(mouseWorldPos, closestPoint));

                    if (Vector3.Distance(mouseWorldPos, closestPoint) < INTERACT_RADIUS)
                    {
                        springsToRemove.Add(spring);
                    }
                }
                
            }
            
            foreach (var deadSpring in springsToRemove)
                RemoveSpring(deadSpring);
        }

        public void AddSpring(Spring spring)
        {
            _springs.Add(spring);
            
            if (!_network.ContainsKey(spring.A)) _network.Add(spring.A, new());
            if (!_network.ContainsKey(spring.B)) _network.Add(spring.B, new());
            
            _network[spring.A].Add(spring.B);
            _network[spring.B].Add(spring.A);
        }

        public void RemoveSpring(Spring spring)
        {
            _springs.Remove(spring);

            _network[spring.A].Remove(spring.B);
            _network[spring.B].Remove(spring.A);
        }
        
        public List<Spring> GetSprings() => _springs;

        public bool SpringExistsBetweenBodies(PhysicsBody a, PhysicsBody b)
        {
            if (!_network.ContainsKey(a)) return false;
            
            return _network[a].Contains(b);
        }

        public PhysicsBody GetMutualConnectionBetweenBodies(PhysicsBody a, PhysicsBody b)
        {
            if (!_network.ContainsKey(a) || !_network.ContainsKey(b)) return null;
            
            foreach (var aConnection in _network[a])
            {
                if (_network[b].Contains(aConnection))
                    return aConnection;
            }

            return null;
        }

        public SpringSettings GetSettings() => _settings;
        public void SetSettings(SpringSettings settings) => _settings = settings;

        private static Vector3 ComputeForce(PhysicsBody a, PhysicsBody b, float restLength, SpringSettings settings)
        {
            if (a == null || b == null)
            {
                Debug.LogWarning("No other body!");
                return new();
            }

            Vector3 aPos = a.transform.position;
            Vector3 bPos = b.transform.position;
            float stiffness = settings.Stiffness;

            Vector3 offset = aPos - bPos;
            float length = offset.magnitude;

            float displacement = length - restLength;
            float springForceMagnitude = stiffness * displacement;
            Vector3 springForceDir = offset.normalized;
            Vector2 springVelocity = b.GetVelocity() - a.GetVelocity();
            Vector3 dampingForce = Vector3.Dot(springForceDir, springVelocity) 
                              * settings.DampingRatio * -springForceDir;

            return springForceDir * springForceMagnitude + dampingForce;
        }

        private void OnDrawGizmos()
        {
            
            foreach (var spring in _springs)
            {
                var aPos = spring.A.transform.position;
                var bPos = spring.B.transform.position;

                var absDiff = Mathf.Abs(spring.RestLength - Vector3.Distance(aPos, bPos));
                Gizmos.color = Color.Lerp(Color.green, Color.red, absDiff / 5f);
                Gizmos.DrawLine(aPos, bPos);
            }

            // if (Input.GetMouseButton((int)MouseButton.LeftMouse))
            // {
            //     var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //     mouseWorldPos.z = 0;
            //     
            //     var a = aPos;
            //     var b = bPos    ;
            //
            //     var distance = Vector3.Distance(a, b);
            //     var direction = (b - a).normalized;
            //     var aDot = Vector3.Dot(direction, a);
            //     var bDot = Vector3.Dot(direction, b);
            //     var lengthAlong = Vector3.Dot(direction, mouseWorldPos);
            //     var closestPoint = Vector3.Lerp(a, b, (Mathf.Clamp(lengthAlong, aDot, bDot) - aDot) / distance);
            //
            //     Gizmos.DrawWireSphere(closestPoint, INTERACT_RADIUS);
            // }
        }
    }
}