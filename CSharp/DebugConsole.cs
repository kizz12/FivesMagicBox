
 
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
 
 
public class DebugConsole : MonoBehaviour {
	
	public Text guiTEXT;
	public string output = "";
    public string stack = "";
	public Button clearBtn;
	public Scrollbar consoleSB;
	
	
    void OnEnable() {
        Application.logMessageReceived += HandleLog;
    }
    void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }
	
	void Start() {
		clearBtn.onClick.AddListener(Clear);
	}
	
	void Clear() {
		guiTEXT.text = "";
	}
	
    void HandleLog(string logString, string stackTrace, LogType type) {
        output = logString;
        stack = stackTrace;
		if (guiTEXT.text.Length > 20000) {
			Clear();
			return;
		}
		if (output.Length > 100) {
			if (output!= "") {
				consoleSB.value = 0;
				guiTEXT.text += ("MSG: "+output.Substring(0, 100)+"...;\n");
				
			}
		} else {
			if (output!= "") {
				consoleSB.value = 0;
				guiTEXT.text += ("MSG: "+output+";\n");
				
			}
		}
    }	
}// End DebugConsole Class