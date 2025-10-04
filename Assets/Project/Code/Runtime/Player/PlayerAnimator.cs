using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator _anim;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    // Public animation triggers (can expand with more states later)
    public void PlayIdle() => _anim.Play("Idle");
    public void PlayWalk() => _anim.Play("Walk");
    public void PlaySprint() => _anim.Play("Sprint");
    public void PlayCrouch() => _anim.Play("Crouch");
    public void PlayJump() => _anim.Play("Jump");
    public void PlayGlide() => _anim.Play("Glide");
    public void PlayClimb() => _anim.Play("Climb");
    public void PlayAir() => _anim.Play("Long Fall");

    public void SetSpeed(float speed) => _anim.speed = speed; // Sets the Animator's playback speed to whatever value is passed in.
                                                              // Example: SetSpeed(1f) = normal, SetSpeed(0f) = paused, SetSpeed(2f) = double speed.
}
