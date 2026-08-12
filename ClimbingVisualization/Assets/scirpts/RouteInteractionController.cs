using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class RouteInteractionController : MonoBehaviour
{
    [Header("Info-Panel")]
    [SerializeField] private GameObject infoPanel;

    [Header("Route-Details")]
    [SerializeField] private TextMeshProUGUI routeNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI exesText;
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Sonstiges")]
    [SerializeField] private LayerMask routeLayerMask;
    [SerializeField] private Camera arCamera;

    // Zustand: sind Desc + Info sichtbar?
    private bool detailsVisible = false;

    void Update()
    {
        Vector2? tapPosition = null;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            tapPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            tapPosition = Mouse.current.position.ReadValue();
        }

        if (tapPosition.HasValue)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            HandleTap(tapPosition.Value);
        }
    }

    private void HandleTap(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, routeLayerMask))
        {
            var handler = hit.collider.GetComponentInParent<SplineHandler>();
            if (handler != null)
            {
                ShowInfo(handler);
                return;
            }
        }

        // Klick ins Leere: Panel ausblenden
        if (infoPanel != null && infoPanel.activeSelf)
        {
            infoPanel.SetActive(false);
        }
    }

    private void ShowInfo(SplineHandler handler)
    {
        if (infoPanel == null)
            return;

        infoPanel.SetActive(true);

        var data = handler.RouteData;
        if (data == null)
            return;

        if (routeNameText != null)
            routeNameText.text = string.IsNullOrEmpty(data.routeName) ? "-" : data.routeName;

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrEmpty(data.description) ? "-" : data.description;

        if (difficultyText != null)
            difficultyText.text = string.IsNullOrEmpty(data.difficulty) ? "-" : data.difficulty;

        if (exesText != null)
            exesText.text = data.exes > 0 ? data.exes.ToString() : "-";

        if (infoText != null)
            infoText.text = string.IsNullOrEmpty(data.info) ? "-" : data.info;

        // Panel initial kompakt: Desc + Info aus
        detailsVisible = false;
        SetDetailsActive(detailsVisible);
    }

    // Wird vom Button auf dem Panel aufgerufen
    public void ToggleDetails()
    {
        detailsVisible = !detailsVisible;
        SetDetailsActive(detailsVisible);
    }

    private void SetDetailsActive(bool active)
    {
        if (descriptionText != null)
            descriptionText.gameObject.SetActive(active);

        if (infoText != null)
            infoText.gameObject.SetActive(active);
    }
}