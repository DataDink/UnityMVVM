using System;
using UnityEngine;
using UnityMVVM;
using UnityMVVM.Base;

public class Application : MonoBehaviour 
{
    private DateTime LastUpdate;
    void Start() { LastUpdate = DateTime.Now; }
    void Update()
    {
        if (DateTime.Now < LastUpdate.AddSeconds(1)) { return; }
        LastUpdate = DateTime.Now;
        try {
          var json = Resources.Load<TextAsset>("data").text;
          var model = ViewModel.Parser.Parse(json);
          var view = GetComponent<View>();
          view.Bind(model);
        } catch (Exception ex) { Debug.LogError(ex); }
    }
}
