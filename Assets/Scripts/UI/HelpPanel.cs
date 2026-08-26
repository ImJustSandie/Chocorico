using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Panel de ayuda paginado: muestra imágenes una por una.
/// Cada click/tap avanza a la siguiente imagen; al pasar la última, el panel se cierra solo.
/// Setup en el Editor:
///   1. El panel raíz debe tener un Image (puede ser transparente) con Raycast Target activado,
///      para que los clicks lleguen a este componente.
///   2. Las Images de cada página deben tener Raycast Target DESACTIVADO para no bloquear el click.
///   3. Arrastrar cada página (hija del panel) al arreglo 'pages' en orden.
/// </summary>
public class HelpPanel : MonoBehaviour, IPointerClickHandler
{
    [Header("Páginas")]
    [Tooltip("Imágenes/páginas de la ayuda, en el orden en que se muestran.")]
    [SerializeField] private GameObject[] pages;

    [Header("Opcional")]
    [Tooltip("Botón X para cerrar el panel en cualquier momento (opcional). Debe llamar a Close().")]
    [SerializeField] private GameObject closeButton;

    private int currentPage = 0;

    /// <summary>
    /// Abre el panel desde la primera imagen. Conectar al botón de Ayuda del menú.
    /// </summary>
    public void Open()
    {
        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        currentPage = 0;
        ShowPage(currentPage);
    }

    /// <summary>
    /// Avanza a la siguiente imagen. Si es la última, cierra el panel.
    /// </summary>
    public void NextPage()
    {
        if (currentPage + 1 >= pages.Length)
        {
            Close();
            return;
        }

        ShowPage(++currentPage);
    }

    /// <summary>
    /// Cierra el panel. Conectar al botón X si se usa uno.
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        NextPage();
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == index);
            }
        }

        if (closeButton != null)
        {
            closeButton.SetActive(true);
        }
    }
}
