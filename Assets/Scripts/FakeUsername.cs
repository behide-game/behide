using System.IO;
using UnityEngine;

class FakeUsername : MonoBehaviour {
    [SerializeField] private string filePath;
    static private string[] usernames;

    void Awake() {
        usernames = File.ReadAllLines(filePath);
    }

    static public string getRandom() {
        int index = Random.Range(0, usernames.Length);
        return usernames[index];
    }
}