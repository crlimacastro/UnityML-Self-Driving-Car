using UnityEngine;

public class FlightControls : MonoBehaviour
{
    public static bool Inverted = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            FlightControls.Inverted = !FlightControls.Inverted;
        }
    }
}
