using UnityEngine;

public class DisplayManager : MonoBehaviour
{
    void Start()
    {
        // Controleer of er meer dan 1 display aangesloten is
        if (Display.displays.Length > 1)
        {
            // Activeer Display 2 (index 1)
            Display.displays[1].Activate();
        }
    }
}