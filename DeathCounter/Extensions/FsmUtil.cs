using HutongGames.PlayMaker;
using Vasi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace DeathCounter.Extensions
{
    public static class FsmUtil
    {
        public static void AddTransition(this PlayMakerFSM fsm, string stateName, string @event, string toState, bool newEvent = false)
        {
            var state = fsm.Fsm.GetState(stateName);

            List<FsmTransition> transitions = state.Transitions.ToList();
            transitions.Add(new FsmTransition()
            {
                ToState = toState,
                FsmEvent = newEvent ? new FsmEvent(@event) : fsm.FsmEvents.FirstOrDefault(x => x.Name == @event)
            });

            state.Transitions = transitions.ToArray();
        }

        public static FsmState CopyState(this PlayMakerFSM fsm, string stateName, string newState)
        {
            var state = new FsmState(fsm.GetState(stateName)) { Name = newState };

            List<FsmState> fsmStates = fsm.FsmStates.ToList();
            fsmStates.Add(state);
            fsm.Fsm.States = fsmStates.ToArray();

            return state;
        }
        public static GameObject FindGameObjectInChildren(this GameObject gameObject, string name)
        {
            bool flag = gameObject == null;
            GameObject result;
            if (flag)
            {
                result = null;
            }
            else
            {
                foreach (Transform transform in gameObject.GetComponentsInChildren<Transform>(true))
                {
                    bool flag2 = transform.name == name;
                    if (flag2)
                    {
                        return transform.gameObject;
                    }
                }
                result = null;
            }
            return result;
        }

    }
}
