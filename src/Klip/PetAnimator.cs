using System;

namespace Klip;

internal sealed class PetAnimator
{
    private readonly Random random = new();

    private IdleAnimation currentAnimation = IdleAnimation.None;
    private long animationStartedAtMs;
    private long nextAnimationAtMs;

    private float moveAngle;
    private float moveScale = 1f;

    private const float MaxTiltAngle = 10f;
    private const float TiltResponse = 0.18f;
    private const float ScaleResponse = 0.12f;
    private const float MaxMoveScaleBoost = 0.06f;
    private const double MaxSpeed = 18.0;

    public float Angle { get; private set; }
    public float Scale { get; private set; } = 1f;

    public PetAnimator()
    {
        ScheduleNextAnimation(0);
    }

    public void Update(long now, double vx, double speed, bool isPaused)
    {
        UpdateIdleAnimation(now);
        UpdateMovementEffects(vx, speed, isPaused);

        Angle += moveAngle;
        Scale *= moveScale;
    }

    private void UpdateIdleAnimation(long now)
    {
        if (currentAnimation == IdleAnimation.None)
        {
            Angle = 0f;
            Scale = 1f;

            if (now >= nextAnimationAtMs)
            {
                currentAnimation = (IdleAnimation)random.Next(1, 4);
                animationStartedAtMs = now;
            }

            return;
        }

        double t = (now - animationStartedAtMs) / 1000.0;

        switch (currentAnimation)
        {
            case IdleAnimation.Shake:
                UpdateShake(t, now);
                break;

            case IdleAnimation.Look:
                UpdateLook(t, now);
                break;

            case IdleAnimation.Bounce:
                UpdateBounce(t, now);
                break;
        }
    }

    private void UpdateMovementEffects(double vx, double speed, bool isPaused)
    {
        if (isPaused)
        {
            moveAngle += -moveAngle * TiltResponse;
            moveScale += (1f - moveScale) * ScaleResponse;
            return;
        }

        float normalizedSpeed = (float)Math.Min(speed / MaxSpeed, 1.0);
        float targetAngle = (float)(vx / MaxSpeed) * MaxTiltAngle;
        float targetScale = 1f + normalizedSpeed * MaxMoveScaleBoost;

        moveAngle += (targetAngle - moveAngle) * TiltResponse;
        moveScale += (targetScale - moveScale) * ScaleResponse;
    }

    private void UpdateShake(double t, long now)
    {
        const double duration = 0.75;
        const double frequency = 10.0;
        const float amplitude = 14f;

        if (t >= duration)
        {
            EndAnimation(now);
            return;
        }

        double falloff = 1.0 - (t / duration);
        Angle = (float)(Math.Sin(t * Math.PI * 2.0 * frequency) * amplitude * falloff);
        Scale = 1f;
    }

    private void UpdateLook(double t, long now)
    {
        const double duration = 2.4;
        const float amplitude = 12f;

        if (t >= duration)
        {
            EndAnimation(now);
            return;
        }

        double phase = t / duration;
        Angle = (float)(Math.Sin(phase * Math.PI * 4.0) * amplitude);
        Scale = 1f;
    }

    private void UpdateBounce(double t, long now)
    {
        const double duration = 1.5;
        const double cycles = 2.5;
        const float amplitude = 0.2f;

        if (t >= duration)
        {
            EndAnimation(now);
            return;
        }

        double wave = Math.Sin((t / duration) * Math.PI * 2.0 * cycles);
        Scale = 1f + (float)(wave * amplitude);
        Angle = 0f;
    }

    private void EndAnimation(long now)
    {
        currentAnimation = IdleAnimation.None;
        Angle = 0f;
        Scale = 1f;
        ScheduleNextAnimation(now);
    }

    private void ScheduleNextAnimation(long now)
    {
        nextAnimationAtMs = now + random.Next(5000, 20001);
    }

    private enum IdleAnimation
    {
        None,
        Shake,
        Look,
        Bounce
    }
}
