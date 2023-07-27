using UnityEngine;

public class FakeUsername : MonoBehaviour {
    [SerializeField] private TextAsset file;
    static private string[] usernames;

    void Awake() {
        usernames = file.text.Split("\r\n");
    }

    static public string getRandom() {
        int index = Random.Range(0, usernames.Length);
        return usernames[index];
    }
}