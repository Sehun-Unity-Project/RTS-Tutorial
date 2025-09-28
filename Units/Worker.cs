using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
using GameDevTV.RTS.EventBus;
using GameDevTV.RTS.Events;

namespace GameDevTV.RTS.Units
{

    // // requires
    // [RequireComponent(typeof(NavMeshAgent))]

    public class Worker : AbstractUnit
    {

        private NavMeshAgent agent;
        [SerializeField] Transform target;
        [SerializeField] DecalProjector decalProjector;

        public void DeSelect()
        {
            if (decalProjector != null)
            {
                decalProjector.gameObject.SetActive(false);
            }
             Bus<UnitSelectedEvent>.Raise(new UnitSelectedEvent(this));
        }

        public void MoveTo(Vector3 position)
        {
            agent.SetDestination(position);
        }

        public void Select()
        {
            if (decalProjector != null)
            {
                decalProjector.gameObject.SetActive(true);
            }

            Bus<UnitSelectedEvent>.Raise(new UnitSelectedEvent(this));
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }
}
