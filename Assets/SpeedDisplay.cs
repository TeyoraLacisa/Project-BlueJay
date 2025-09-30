using UnityEngine;
using TMPro;

public class SpeedDisplay : MonoBehaviour
{
    [Header("Settings")]
    public PlayerController playerController;
    public TextMeshProUGUI speedText;
    public string unit = "u/s";
    
    [Header("Coordinate Settings")]
    public bool showCoordinates = true;
    public string coordinateFormat = "F1";
    public bool showYCoordinate = true;
    
    void Update()
    {
        if (playerController == null || speedText == null) return;
        
        // Get speed from player controller
        float speed = playerController.GetVelocity().magnitude;
        
        // Create display text
        string displayText = $"Speed: {speed.ToString("F1")} {unit}";
        
        // Add coordinates if enabled
        if (showCoordinates)
        {
            Vector3 position = playerController.transform.position;
            if (showYCoordinate)
            {
                displayText += $"\nPosition:\nX: {position.x.ToString(coordinateFormat)}\nY: {position.y.ToString(coordinateFormat)}\nZ: {position.z.ToString(coordinateFormat)}";
            }
            else
            {
                displayText += $"\nPosition:\nX: {position.x.ToString(coordinateFormat)}\nZ: {position.z.ToString(coordinateFormat)}";
            }
        }
        
        // Update text
        speedText.text = displayText;
    }
}