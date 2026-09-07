using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CreateAccountCancleButton : MonoBehaviour
{
    [SerializeReference]
    TMP_InputField m_idField;
    [SerializeReference]
    TMP_InputField m_pwField;
    [SerializeReference]
    GameObject m_accountCreatePanel;
    public void OnClick()
    {
        m_idField.text = "";
        m_pwField.text = "";
        m_accountCreatePanel.SetActive(false);
    }
}
