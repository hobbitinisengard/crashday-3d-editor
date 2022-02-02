using UnityEngine;

class ExecutionOrderWorkaround : MonoBehaviour
{
	/// <summary>
	/// An invisible full-screen panel that triggers Mouse Input UI Blocker
	/// </summary>
	public GameObject EscapeConfirmationPanel;
	public GameObject EscapeConfirmationWindow;

	void Update()
	{
		if (EscapeConfirmationPanel.activeSelf && !EscapeConfirmationWindow.activeSelf)
			EscapeConfirmationPanel.SetActive(false);
	}
}
