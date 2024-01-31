using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityMVVM;

public class Application : MonoBehaviour 
{
    private DateTime LastUpdate;
    // Start is called before the first frame update
    void Start()
    {
      LastUpdate = DateTime.Now;
    }

    // Update is called once per frame
    void Update()
    {
        if (DateTime.Now < LastUpdate.AddSeconds(1)) { return; }
        LastUpdate = DateTime.Now;
        try {
          var json = Resources.Load<TextAsset>("data").text;
          var model = Model.Parser.Parse(json);
          var view = GetComponent<View>();
          view.Bind(model);
        } catch (Exception ex) { Debug.LogError(ex); }
    }
}
