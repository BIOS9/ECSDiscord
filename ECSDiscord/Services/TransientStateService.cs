using System;
using System.Collections.Concurrent;

namespace ECSDiscord.Services
{
    public class TransientStateService
    {
        public class UserState
        {
            public readonly ulong UserId;
            public readonly bool Real;
            public readonly bool Exists;
            public readonly DateTime? LeftExistenceTime; // Really should not use nullable here. Constructor will not null this.
            public readonly int RealityState;

            public UserState(
                ulong userId,
                bool real = true,
                bool exists = true,
                DateTime? leftExistenceTime = null,
                int realityState = 0)
            {
                UserId = userId;
                Real = real;
                Exists = exists;
                LeftExistenceTime = leftExistenceTime;
                RealityState = realityState;
            }

            public UserState(
                UserState oldState,
                bool? real = null,
                bool? exists = null,
                DateTime? leftExistenceTime = null,
                int? realityState = null) :
                this(
                oldState.UserId,
                real ?? oldState.Real,
                exists ?? oldState.Exists,
                leftExistenceTime ?? oldState.LeftExistenceTime,
                realityState ?? oldState.RealityState)
            { }

        }

        private readonly ConcurrentDictionary<ulong, UserState> _userStates = new ConcurrentDictionary<ulong, UserState>();

        public void SetUserState(ulong userId, UserState state)
        {
            _userStates[userId] = state;
        }

        public UserState GetUserState(ulong userId)
        {
            return _userStates.GetOrAdd(userId, x => new UserState(x));
        }
    }
}