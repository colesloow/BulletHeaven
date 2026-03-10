using System.Collections;
using UnityEngine;

public class LaserBeamController : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField]
    private LineRenderer _laserLine;
    [SerializeField]
    private BoxCollider _laserCollider;
    [SerializeField]
    private float _laserMaxLength = 5f;
    [SerializeField]
    private float _laserExpandSpeed = 5f;
    [SerializeField]
    private float _laserDuration = 5f;
    [SerializeField]
    private float _laserInterval = 5f;

    private Coroutine _laserCoroutine;
    private bool _laserUnlocked = false;
    [SerializeField] 
    private bool _autoFireActive = true;


    private void Start()
    {
        GameManager.Instance.OnLaserUnlocked += UnlockLaser; // subscribe to laser unlocked event
        GameManager.Instance.OnLaserLevelUp += LevelUpLaser;

        _laserCollider.enabled = false;

        if (_autoFireActive)
        {
            StartCoroutine(AutoFireLaser()); // start automatic firing loop
        }
    }

    private void Update()
    {
        if (!_autoFireActive) // allows to fire laser with spacebar
        {
            // start laser if player presses space
            if (Input.GetKeyDown(KeyCode.Space) )
            {
                StartLaser();
            }

            // stop laser if button released
            if (Input.GetKeyUp(KeyCode.Space))
            {
                StopLaser();
            }
        }
    }

    private void UnlockLaser(bool unlocked)
    {
        _laserUnlocked = unlocked;
    }
    
    private void LevelUpLaser(float intervalAmount, float durationAmount)
    {
        _laserInterval -= intervalAmount;
        _laserDuration += durationAmount;
    }

    private void StartLaser()
    {
        // stop any existing laser coroutine before starting a new one
        if (_laserCoroutine != null)
        {
            StopCoroutine(_laserCoroutine);
        }

        _laserCoroutine = StartCoroutine(ShootLaser());
    }

    public void StopLaser()
    {
        // stop any existing laser coroutine before retracting
        if (_laserCoroutine != null)
        {
            StopCoroutine(_laserCoroutine);
        }
        _laserCoroutine = StartCoroutine(RetractLaser());
    }

    private IEnumerator AutoFireLaser()
    {
        while (true) // infinite loop to fire laser at intervals
        {
            yield return new WaitForSeconds(_laserInterval);

            if (_laserUnlocked)
            {
                StartLaser();
            }
        }
    }

    private IEnumerator ShootLaser()
    {
        _laserLine.enabled = true;
        _laserCollider.enabled = true;

        // initialize laser positions
        Vector3[] positions = new Vector3[2];
        positions[0] = Vector3.zero;
        positions[1] = Vector3.zero;

        // play laser sound
        SoundManager.PlaySound(SoundType.LASER);

        _laserLine.SetPositions(positions);

        float currentLength = 0f;

        // expand the laser forward
        while (currentLength < _laserMaxLength)
        {
            currentLength += _laserExpandSpeed * Time.deltaTime;
            positions[1].z = Mathf.Min(currentLength, _laserMaxLength);

            _laserLine.SetPositions(positions);

            UpdateCollider(currentLength);

            yield return null;
        }

        StopLaser();
    }

    private IEnumerator RetractLaser()
    {
        Vector3[] positions = new Vector3[2];

        // retrieve current laser positions
        _laserLine.GetPositions(positions);

        float currentStart = positions[0].z;
        float currentEnd = positions[1].z;

        // retract laser by moving its start position forward
        while (currentStart < currentEnd)
        {
            currentStart += _laserExpandSpeed * Time.deltaTime;
            positions[0].z = Mathf.Min(currentStart, currentEnd);

            _laserLine.SetPositions(positions);

            UpdateCollider(currentEnd - currentStart);

            yield return null;
        }

        // disable the laser once retracted
        _laserLine.enabled = false;
        _laserCollider.enabled = false;
    }

    private void UpdateCollider(float length)
    {
        // adjust collider to fit the current laser length
        _laserCollider.center = new Vector3(0, 0, length / 2); // center on z axis
        _laserCollider.size = new Vector3(0.001f, 0.001f, length); // very small size on x and y
    }
}
