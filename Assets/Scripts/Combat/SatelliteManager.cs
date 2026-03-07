using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SatelliteManager : MonoBehaviour
{
    [SerializeField]
    private int _satelliteCount = 1;
    public int SatelliteCount 
    {
        get => _satelliteCount; 
        private set => _satelliteCount = Mathf.Clamp(value, 0, _maxSatellites);
    }

    [SerializeField] 
    private GameObject _satellitePrefab;
    [SerializeField] 
    private Transform _satellitesParent;
    [SerializeField]
    [Range(0, 10)] 
    private int _maxSatellites = 10;
    [SerializeField]
    [Range(0.2f, 10.0f)] 
    private float _orbitRadius = 2.0f;

    private GameObject[] _satellites;

    private void Start()
    {
        GameManager.Instance.OnSatelliteGained += AddSatellite; // subscribe to satellite gain event
        UpdateSatellites();
    }


    // method called when values are changed in the inspector
    private void OnValidate()
    {
        // clamp satellites number to stay in limits
        _satelliteCount = Mathf.Clamp(_satelliteCount, 0, _maxSatellites);

        // check if app is playing to avoid errors
        if (Application.isPlaying)
        {
            UpdateSatellites();
        }
    }

    private void AddSatellite(int amount)
    {
        if (_satelliteCount < _maxSatellites)
        {
            _satelliteCount += amount;
            UpdateSatellites();
        }
    }

    public void UpdateSatellites()
    {
        // destroy old satellites if they exist
        if (_satellites != null)
        {
            foreach (var satellite in _satellites)
            {
                if (satellite != null)
                {
                    Destroy(satellite);
                }
            }
        }

        _satellites = new GameObject[_satelliteCount];

        // place satellites regularly around robot
        for (int i = 0; i < _satelliteCount; i++)
        {
            // angle for each satellite
            float angle = i * 360f / _satelliteCount;
            Vector3 position = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * _orbitRadius,
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad) * _orbitRadius
            );

            if (_satellitesParent == null) return;
            GameObject newSatellite = Instantiate(_satellitePrefab, _satellitesParent);
            newSatellite.transform.localPosition = position;

            // adjust X & Y rotation
            Quaternion baseRotation = Quaternion.Euler(90, 90, 0);
            Quaternion angleRotation = Quaternion.Euler(0, -angle, 0);
            newSatellite.transform.localRotation = angleRotation * baseRotation;

            _satellites[i] = newSatellite;
        }
    }

    public void StopSatellites()
    {
        // stop rotation
        _satellitesParent.gameObject.GetComponent<SatelliteRotator>().StopRotation();

        foreach (var satellite in _satellites)
        {
            Rigidbody rb = satellite.GetComponent<Rigidbody>();
            rb.isKinematic = false;

            // disable lasers
            LaserBeamController laserBeam = satellite.GetComponent<LaserBeamController>();
            laserBeam.StopLaser();
        }
    }
}
