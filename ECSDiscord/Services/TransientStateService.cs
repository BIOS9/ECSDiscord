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

            public UserState(
                ulong userId,
                bool real = true,
                bool exists = true)
            {
                UserId = userId;
                Real = real;
                Exists = exists;
            }

            public UserState(
                UserState oldState,
                bool? real,
                bool? exists) :
                this(
                oldState.UserId,
                real ?? oldState.Real,
                exists ?? oldState.Exists)
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