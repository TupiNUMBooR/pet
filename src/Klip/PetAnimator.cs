using System;

namespace Klip;

internal sealed class PetAnimator
{
    private readonly Random random = new();

    private IdleAnimation currentAnimation = IdleAnimation.None;
    private long animationStartedAtMs;
    private long nextAnimationAtMs;

    private float baseAngle;
    private float baseScale = 1f;

    private float motionAngle;
    private float motionScale = 1f;

    private const double MaxSpeed = 18.0;

    private const float MaxTiltAngle = 10f;
    private const float TiltResponse = 0.18f;
    private const float ScaleResponse = 0.12f;
    private const float MaxMoveScaleBoost = 0.06f;

    public float Angle { get; private set; }
    public float Scale { get; private set; } = 1f;

    public PetAnimator()
    {
        ScheduleNextAnimation(0);
    }

    public void Update(long now, double vx, double speed, bool isPaused)
    {
        UpdateBaseAnimation(now);
        UpdateMotionAnimation(vx, speed, isPaused);

        Angle = baseAngle + motionAngle;
        Scale = baseScale * motionScale;
    }

    private void UpdateBaseAnimation(long now)
    {
        if (currentAnimation == IdleAnimation.None)
        {
            baseAngle = 0f;
            baseScale = 1f;

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

    private void UpdateMotionAnimation(double vx, double speed, bool isPaused)
    {
        if (isPaused)
        {
            motionAngle += -motionAngle * TiltResponse;
            motionScale += (1f - motionScale) * ScaleResponse;
            return;
        }

        float normalizedSpeed = (float)Math.Min(speed / MaxSpeed, 1.0);
        float targetAngle = (float)(vx / MaxSpeed) * MaxTiltAngle;
        float targetScale = 1f + normalizedSpeed * MaxMoveScaleBoost;

        motionAngle += (targetAngle - motionAngle) * TiltResponse;
        motionScale += (targetScale - motionScale) * ScaleResponse;
    }

    private void UpdateShake(double t, long now)
    {
        const double duration = 0.9;
        const double frequency = 10.0;
        const float amplitude = 14f;

        if (t >= duration)
        {
            EndAnimation(now);
            return;
        }

        float intensity = GetFadeIntensity(t, duration, 0.18);
        baseAngle = (float)(Math.Sin(t * Math.PI * 2.0 * frequency) * amplitude * intensity);
        baseScale = 1f;
    }

    private void UpdateLook(double t, long now)
    {
        const double duration = 2.6;
        const float amplitude = 12f;

        if (t >= duration)
        {
            EndAnimation(now);
            return;
        }

        double phase = t / duration;
        float intensity = GetFadeIntensity(t, duration, 0.22);

        baseAngle = (float)(Math.Sin(phase * Math.PI * 4.0) * amplitude * intensity);
        baseScale = 1f;
    }

    private void UpdateBounce(double t, long now)
    {
        const double duration = 1.7;
        const double cycles = 2.5;
        const float amplitude = 0.2f;

        if (t >= duration)
        {
            EndAnimation(now);
            return;
        }

        double wave = Math.Sin((t / duration) * Math.PI * 2.0 * cycles);
        float intensity = GetFadeIntensity(t, duration, 0.2);

        baseScale = 1f + (float)(wave * amplitude * intensity);
        baseAngle = 0f;
    }

    private static float GetFadeIntensity(double t, double duration, double fadeFraction)
    {
        double fadeDuration = duration * fadeFraction;

        if (fadeDuration <= 0.0)
        {
            return 1f;
        }

        double fadeIn = Math.Min(t / fadeDuration, 1.0);
        double fadeOut = Math.Min((duration - t) / fadeDuration, 1.0);
        double intensity = Math.Min(fadeIn, fadeOut);

        return SmoothStep((float)intensity);
    }

    private static float SmoothStep(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private void EndAnimation(long now)
    {
        currentAnimation = IdleAnimation.None;
        baseAngle = 0f;
        baseScale = 1f;
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
