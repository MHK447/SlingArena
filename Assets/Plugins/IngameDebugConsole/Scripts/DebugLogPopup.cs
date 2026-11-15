using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;
using UnityEngine.EventSystems;
using System.Collections;

// Manager class for the debug popup
namespace IngameDebugConsole
{
	public class DebugLogPopup : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private RectTransform popupTransform;

		// Dimensions of the popup divided by 2
		private Vector2 halfSize;

		// Background image that will change color to indicate an alert
		private Image backgroundImage;

		// Canvas group to modify visibility of the popup
		private CanvasGroup canvasGroup;

#pragma warning disable 0649
		[SerializeField]
		private DebugLogManager debugManager;

		[SerializeField]
		private Text newInfoCountText;
		[SerializeField]
		private Text newWarningCountText;
		[SerializeField]
		private Text newErrorCountText;

		[SerializeField]
		private Color alertColorInfo;
		[SerializeField]
		private Color alertColorWarning;
		[SerializeField]
		private Color alertColorError;
#pragma warning restore 0649

		// Number of new debug entries since the log window has been closed
		private int newInfoCount = 0, newWarningCount = 0, newErrorCount = 0;

		private Color normalColor;

		private bool isPopupBeingDragged = false;
		private Vector2 normalizedPosition;

		// The popup is pinned to the position set in the prefab/scene (e.g. top-left corner)
		private Vector2 initialAnchoredPosition;

		// Coroutines for simple code-based animations
		private IEnumerator moveToPosCoroutine = null;

		private void Awake()
		{
			popupTransform = (RectTransform) transform;
			backgroundImage = GetComponent<Image>();
			canvasGroup = GetComponent<CanvasGroup>();

			normalColor = backgroundImage.color;

			halfSize = popupTransform.sizeDelta * 0.5f;

			initialAnchoredPosition = popupTransform.anchoredPosition;

			Vector2 pos = popupTransform.anchoredPosition;
			if( pos.x != 0f || pos.y != 0f )
				normalizedPosition = pos.normalized; // Respect the initial popup position set in the prefab
			else
				normalizedPosition = new Vector2( 0.5f, 0f ); // Right edge by default
		}

		public void NewLogsArrived( int newInfo, int newWarning, int newError )
		{
			if( newInfo > 0 )
			{
				newInfoCount += newInfo;
				newInfoCountText.text = newInfoCount.ToString();
			}

			if( newWarning > 0 )
			{
				newWarningCount += newWarning;
				newWarningCountText.text = newWarningCount.ToString();
			}

			if( newError > 0 )
			{
				newErrorCount += newError;
				newErrorCountText.text = newErrorCount.ToString();
			}

			if( newErrorCount > 0 )
				backgroundImage.color = alertColorError;
			else if( newWarningCount > 0 )
				backgroundImage.color = alertColorWarning;
			else
				backgroundImage.color = alertColorInfo;
		}

		private void Reset()
		{
			newInfoCount = 0;
			newWarningCount = 0;
			newErrorCount = 0;

			newInfoCountText.text = "0";
			newWarningCountText.text = "0";
			newErrorCountText.text = "0";

			backgroundImage.color = normalColor;
		}

		// A simple smooth movement animation
		private IEnumerator MoveToPosAnimation( Vector2 targetPos )
		{
			float modifier = 0f;
			Vector2 initialPos = popupTransform.anchoredPosition;

			while( modifier < 1f )
			{
				modifier += 4f * Time.unscaledDeltaTime;
				popupTransform.anchoredPosition = Vector2.Lerp( initialPos, targetPos, modifier );

				yield return null;
			}
		}

		// Popup is clicked
		public void OnPointerClick( PointerEventData data )
		{
			// Hide the popup and show the log window
			if( !isPopupBeingDragged )
				debugManager.ShowLogWindow();
		}

		// Hides the log window and shows the popup
		public void Show()
		{
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
			canvasGroup.alpha = 1f;

			// Reset the counters
			Reset();

			// Update position in case resolution was changed while the popup was hidden
			UpdatePosition( true );
		}

		// Hide the popup
		public void Hide()
		{
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
			canvasGroup.alpha = 0f;

			isPopupBeingDragged = false;
		}

		public void OnBeginDrag( PointerEventData data )
		{
			isPopupBeingDragged = true;

			// If a smooth movement animation is in progress, cancel it
			if( moveToPosCoroutine != null )
			{
				StopCoroutine( moveToPosCoroutine );
				moveToPosCoroutine = null;
			}
		}

		// Reposition the popup
		public void OnDrag( PointerEventData data )
		{
			Vector2 localPoint;
			if( RectTransformUtility.ScreenPointToLocalPointInRectangle( debugManager.canvasTR, data.position, data.pressEventCamera, out localPoint ) )
				popupTransform.anchoredPosition = localPoint;
		}

		// Smoothly translate the popup to the nearest edge
		public void OnEndDrag( PointerEventData data )
		{
			isPopupBeingDragged = false;
			UpdatePosition( false );
		}

		public void UpdatePosition( bool immediately )
		{
			Vector2 canvasSize = debugManager.canvasTR.rect.size;

			float canvasWidth = canvasSize.x;
			float canvasHeight = canvasSize.y;

			// Pin to the position set in the prefab/scene (left corner) regardless of drag end position.
			Vector2 pos = initialAnchoredPosition;

			normalizedPosition.Set( canvasWidth != 0f ? pos.x / canvasWidth : 0f, canvasHeight != 0f ? pos.y / canvasHeight : 0f );

			// If another smooth movement animation is in progress, cancel it
			if( moveToPosCoroutine != null )
			{
				StopCoroutine( moveToPosCoroutine );
				moveToPosCoroutine = null;
			}

			if( immediately )
				popupTransform.anchoredPosition = pos;
			else
			{
				// Smoothly translate the popup to the specified position
				moveToPosCoroutine = MoveToPosAnimation( pos );
				StartCoroutine( moveToPosCoroutine );
			}
		}
	}
}