using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "MYFolder/UI/공통 테마")]
public class UITheme : ScriptableObject
{
	public Sprite		panelSprite;
	public Sprite		buttonSprite;
	public Color		panelColor = new Color(0.25f, 0.25f, 0.25f, 0.98f);
	public Color		insetColor = new Color(0.14f, 0.14f, 0.14f, 0.94f);
	public Color		borderColor = new Color(0.55f, 0.55f, 0.55f, 0.9f);
	public Color		buttonColor = new Color(0.88f, 0.88f, 0.88f, 1.0f);
	public Color		inputColor = new Color(0.94f, 0.94f, 0.94f, 1.0f);
	public Color		textColor = Color.white;
	public Color		buttonTextColor = new Color(0.08f, 0.08f, 0.08f, 1.0f);
	public Color		placeholderColor = new Color(0.38f, 0.38f, 0.38f, 1.0f);
	public Color		overlayColor = new Color(0, 0, 0, 0.65f);

	public ColorBlock GetButtonColors()
	{
		ColorBlock colors = ColorBlock.defaultColorBlock;
		colors.normalColor = Color.white;
		colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1);
		colors.pressedColor = new Color(0.68f, 0.68f, 0.68f, 1);
		colors.selectedColor = Color.white;
		colors.disabledColor = new Color(0.58f, 0.58f, 0.58f, 0.85f);
		colors.fadeDuration = 0.08f;
		return colors;
	}
}
