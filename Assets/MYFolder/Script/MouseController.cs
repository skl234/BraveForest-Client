using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum eMouseBtnType : byte
{
	LEFT = 0,
	Right = 1,
	Middle = 2,
}
public class MouseController
{
	public static bool GetMouseButton(eMouseBtnType _type)
	{
		return Input.GetMouseButton((byte)_type);
	}
	public static bool GetMouseButtonDown(eMouseBtnType _type)
	{
		return Input.GetMouseButtonDown((byte)_type);
	}
	public static bool GetMouseButtonUp(eMouseBtnType _type)
	{
		return Input.GetMouseButtonUp((byte)_type);
	}
	public static Vector3 GetMousePosition()
	{
		return Input.mousePosition;
	}
	public static Ray GetScreenPointToRay(Camera _camera)
	{
		return _camera.ScreenPointToRay(GetMousePosition());
	}
}
