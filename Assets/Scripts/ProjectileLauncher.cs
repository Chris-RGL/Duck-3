using System;
using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject projectilePrefab;
    public Transform firingPoint;

    [Header("Launch Settings")]
    [Range(0f, 70f)]
    public float minAngle = 0f;
    [Range(0f, 70f)]
    public float maxAngle = 70f;
    public float minPower = 5f;
    public float maxPower = 20f;

    [Header("Agent Integration")]
    [Tooltip("Set true when a CatcherAgent is driving the episode lifecycle — disables auto-fire on start and after each result")]
    public bool agentControlled = false;

    /// Fires once per launch with the spawned Projectile; used by CatcherAgent to track the active ball.
    public Action<Projectile> onProjectileLaunched;

    /// Launch angle chosen by the last PrepareShot() call — read by CatcherAgent as an observation.
    public float PreparedAngle { get; private set; }
    /// Launch power chosen by the last PrepareShot() call — read by CatcherAgent as an observation.
    public float PreparedPower { get; private set; }

    private float _launchAngle;
    private bool _readyToFire = true;

    private void Start()
    {
        if (!agentControlled)
            Fire();
    }

    void Update()
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, _launchAngle);
    }

    /// Randomise angle and power for the next shot without spawning anything.
    /// Call from CatcherAgent.OnEpisodeBegin so the agent can observe the params before the ball flies.
    public void PrepareShot()
    {
        PreparedAngle = UnityEngine.Random.Range(minAngle, maxAngle);
        PreparedPower = UnityEngine.Random.Range(minPower, maxPower);
        _launchAngle = PreparedAngle;
        _readyToFire = true;
    }

    /// Spawn and launch the projectile using the values stored by the last PrepareShot() call.
    public void FirePrepared()
    {
        if (!_readyToFire || projectilePrefab == null || firingPoint == null) return;

        _readyToFire = false;
        transform.localRotation = Quaternion.Euler(0f, 0f, _launchAngle);

        GameObject projectileGO = Instantiate(projectilePrefab, firingPoint.position, firingPoint.rotation);

        Rigidbody rb = projectileGO.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(firingPoint.right * PreparedPower, ForceMode.Impulse);

        Projectile proj = projectileGO.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.onResult = OnProjectileResult;
            onProjectileLaunched?.Invoke(proj);
        }
    }

    public void Fire()
    {
        if (!_readyToFire || projectilePrefab == null || firingPoint == null) return;

        _readyToFire = false;
        _launchAngle = UnityEngine.Random.Range(minAngle, maxAngle);
        float power = UnityEngine.Random.Range(minPower, maxPower);

        transform.localRotation = Quaternion.Euler(0f, 0f, _launchAngle);

        GameObject projectileGO = Instantiate(projectilePrefab, firingPoint.position, firingPoint.rotation);

        Rigidbody rb = projectileGO.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(firingPoint.right * power, ForceMode.Impulse);

        Projectile proj = projectileGO.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.onResult = OnProjectileResult;
            onProjectileLaunched?.Invoke(proj);
        }
    }

    private void OnProjectileResult(bool caught)
    {
        _readyToFire = true;
        if (!agentControlled)
            Fire();
    }

    void OnValidate()
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, _launchAngle);
    }
}
