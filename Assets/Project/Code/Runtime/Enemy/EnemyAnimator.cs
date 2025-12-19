using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    private Animator _anim;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    //Public anim triggers.
    public void PlayIdle() => _anim.Play("Idle");
    public void PlayWalk() => _anim.Play("Walk");
    public void PlayRun() => _anim.Play("Run");
    public void PlaySurpised() => _anim.Play("Surprised");

}
