#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX //WindowsかMacでのみ使えるように
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// PC版でウィンドウサイズを自動で調整するクラス
/// </summary>
public class WindowSizeAdjuster : MonoBehaviour
{
#if !UNITY_EDITOR //エディタ上では調整しないように
    //ターゲットのアス比率
    private static readonly float SCREEN_RATE = 16f / 9f;

    //スクリーンサイズ
    private int _width, _height;

    //更新間隔
    private static readonly float UPDATE_SPAN = 1f;
    private float _updateSpan = UPDATE_SPAN;

    //=================================================================================
    //初期化
    //=================================================================================

    private void Start()
    {
        _width = Screen.width;
        _height = Screen.height;
    }

    //=================================================================================
    //更新
    //=================================================================================

    private void Update()
    {

    _updateSpan -= Time.fixedDeltaTime;
    if (_updateSpan > 0) {
      return;
    }
    _updateSpan = UPDATE_SPAN;
    
    //現在のアス比チェック
    int width = Screen.width, height = Screen.height;
    var rate = width / (float)height;
    if(Mathf.Abs(SCREEN_RATE - rate) <= 0.1f) {
      _width  = width;
      _height = height;
      return;
    }
    
    //動かした方に合わせて調整
    if (Mathf.Abs(_width - width) > Mathf.Abs(_height - height)) {
      Screen.SetResolution(width, Mathf.RoundToInt(width / SCREEN_RATE), false);
    }
    else {
      Screen.SetResolution(Mathf.RoundToInt(height * SCREEN_RATE), height, false);
    }
    
    _width  = width;
    _height = height;

}
#endif

}

#endif