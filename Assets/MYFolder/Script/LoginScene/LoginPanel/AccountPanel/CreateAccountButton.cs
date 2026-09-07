using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateAccountButton : MonoBehaviour
{
    [SerializeReference]
    GameObject m_accountCreatePanel;
    public void OnClick()
    {
        m_accountCreatePanel.SetActive(true);
    }
}
