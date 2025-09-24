using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

namespace GameDevTV.RTS.Units
{

    // requires
    [RequireComponent(typeof(NavMeshAgent))]

    public class Worker : MonoBehaviour , ISelectable, IMovable
    {

        private NavMeshAgent agent;
        [SerializeField]Transform target;
        [SerializeField] DecalProjector decalProjector;

        public void DeSelect()
        {
            if (decalProjector != null) {
                decalProjector.gameObject.SetActive(false);
            }
        }

        public void MoveTo(Vector3 position)
        {
             agent.SetDestination(position);
        }

        public void Select()
        {
            if (decalProjector != null) {
                decalProjector.gameObject.SetActive(true);
            }
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }
}
