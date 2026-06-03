using UnityEngine;

namespace NtutAR.Guide
{
    [RequireComponent(typeof(Animator))]
    public sealed class NpcAnimator : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
        }

        public void PlayGreet() => _animator.SetTrigger("Greet");
        public void PlayListening() => _animator.SetTrigger("Listening");
        public void PlayTalk() => _animator.SetTrigger("Talk");

        public void OnNpcState(NpcState state)
        {
            if (state == NpcState.Talking) PlayTalk();
            else PlayListening();
        }
    }
}
