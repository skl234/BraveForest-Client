using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using System.Net.Sockets;

public class LoginButton : MonoBehaviour
{
    [SerializeReference]
    TMP_InputField m_idField;
    [SerializeReference]
    TMP_InputField m_pwField;
    [SerializeReference]
    LoginSceneNetwork m_network;

    public void OnLoginClick()
    {
        string id = m_idField.text;
        string pw = m_pwField.text;

        if (!IsValid(id) || !IsValid(pw))
        {
            Debug.Log("영문 또는 숫자만 기입 바랍니다.");
            return;
        }

        m_network.RequestLogin(id, pw);
    }

    private bool IsValid(string _str)
    {
        if (string.IsNullOrEmpty(_str) || _str.Length > 15) return false;
        foreach (char c in _str)
        {
            if (!(char.IsLetterOrDigit(c))) // 영문자 또는 숫자가 아니면 false
                return false;
        }
        return true;
    }
}
