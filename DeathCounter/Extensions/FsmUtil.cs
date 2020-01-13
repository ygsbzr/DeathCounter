using HutongGames.PlayMaker;
using ModCommon.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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


    }
}
