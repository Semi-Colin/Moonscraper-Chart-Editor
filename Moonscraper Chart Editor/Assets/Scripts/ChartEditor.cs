﻿#define TIMING_DEBUG
//#undef UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System;

[RequireComponent(typeof(AudioSource))]
public class ChartEditor : MonoBehaviour {
    const int POOL_SIZE = 100;

    [Header("Prefabs")]
    public GameObject note;
    public GameObject section;
    public GameObject timeSignatureLine;
    [Header("Indicator Parents")]
    public GameObject guiIndicators;
    [Header("Song properties Display")]
    public UnityEngine.UI.Text songNameText;
    [Header("Misc.")]
    public UnityEngine.UI.Button play;
    public Transform strikeline;
    public TimelineHandler timeHandler;
    public Transform camYMin;
    public Transform camYMax;

    public uint minPos { get; private set; }
    public uint maxPos { get; private set; }
    AudioSource musicSource;

    public Song currentSong { get; private set; }
    public Chart currentChart { get; private set; }
    string currentFileName = string.Empty;

    MovementController movement;
    GameObject[] timeSignatureLinePool = new GameObject[POOL_SIZE];
    GameObject timeSignatureLineParent;

    string lastLoadedFile = string.Empty;

    GameObject songObjectParent;
    GameObject chartObjectParent;

    OpenFileName openFileDialog;
    OpenFileName saveFileDialog;

#if !UNITY_EDITOR
    SaveFileDialog saveDialog;
#endif

    // Use this for initialization
    void Awake () {
        minPos = 0;
        maxPos = 0;

#if !UNITY_EDITOR
        saveDialog = new SaveFileDialog();
        saveDialog.InitialDirectory = "";
        saveDialog.RestoreDirectory = true;
#endif

        openFileDialog = new OpenFileName();

        openFileDialog.structSize = Marshal.SizeOf(openFileDialog);

        openFileDialog.file = new String(new char[256]);
        openFileDialog.maxFile = openFileDialog.file.Length;

        openFileDialog.fileTitle = new String(new char[64]);
        openFileDialog.maxFileTitle = openFileDialog.fileTitle.Length;

        openFileDialog.initialDir = "";
        openFileDialog.title = "Open file";
        openFileDialog.defExt = "txt";

        // Create grouping objects to make reading the inspector easier
        songObjectParent = new GameObject();
        songObjectParent.name = "Song Objects";
        songObjectParent.tag = "Song Object";

        chartObjectParent = new GameObject();
        chartObjectParent.name = "Chart Objects";
        chartObjectParent.tag = "Chart Object";

        // Create a default song
        currentSong = new Song();
        currentChart = currentSong.expert_single;
        musicSource = GetComponent<AudioSource>();

        movement = GameObject.FindGameObjectWithTag("Movement").GetComponent<MovementController>();

        // Initialize object pool
        timeSignatureLineParent = new GameObject("Time Signature Lines");
        for (int i = 0; i < POOL_SIZE; ++i)
        {
            timeSignatureLinePool[i] = Instantiate(timeSignatureLine);
            timeSignatureLinePool[i].transform.SetParent(timeSignatureLineParent.transform);
            timeSignatureLinePool[i].SetActive(false);
        }
    }

    void Update()
    {
        Shortcuts();

        // Update object positions that supposed to be visible into the range of the camera
        minPos = currentSong.WorldYPositionToChartPosition(camYMin.position.y);
        maxPos = currentSong.WorldYPositionToChartPosition(camYMax.position.y);

        // Update time signature lines SNAPPED
        uint snappedLinePos = Snapable.ChartPositionToSnappedChartPosition(minPos, 4);
        int i = 0;
        while (snappedLinePos < maxPos && i < timeSignatureLinePool.Length)
        {
            timeSignatureLinePool[i].SetActive(true);
            timeSignatureLinePool[i].transform.position = new Vector3(0, currentSong.ChartPositionToWorldYPosition(snappedLinePos), 0);
            snappedLinePos += Globals.FULL_STEP / 4;
            ++i;
        }

        // Disable any unused lines
        while (i < timeSignatureLinePool.Length)
        {
            timeSignatureLinePool[i++].SetActive(false);
        }

        //Debug.Log(currentSong.ChartPositionToTime(maxPos) + ", " + currentSong.length);
    }

    void Shortcuts()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightCommand))
        {
            if (Input.GetKeyDown("s"))
                Save();
            else if (Input.GetKeyDown("o"))
                LoadSong();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && Globals.applicationMode == Globals.ApplicationMode.Playing)
            Play();
        else
            musicSource.Stop();
    }

    void OnApplicationQuit()
    {
        while (currentSong.IsSaving) ;

        // Check for unsaved changes
        Debug.Log("Quit");
    }

    public void New()
    {
        openFileDialog.filter = "Audio files\0*.mp3\0*.ogg\0.wav";

#if UNITY_EDITOR
        UnityEditor.EditorUtility.OpenFilePanel("Select Audio", "", "*.mp3;*.ogg;*.wav");
#else
        if (LibWrap.GetOpenFileName(openFileDialog))
        {
            currentFileName = openFileDialog.file;
        }
#endif
    }

    // Wrapper function
    public void LoadSong()
    {
        Stop();
        StartCoroutine(_LoadSong());
    }

    public void Save()
    {
        if (lastLoadedFile != string.Empty)
            Save(lastLoadedFile);
        else
            SaveAs();
    }

    public void SaveAs()
    {
        try {
            string fileName;
#if UNITY_EDITOR
            fileName = UnityEditor.EditorUtility.SaveFilePanel("Save as...", "", currentSong.name, "chart");
#else
            saveDialog.Filter = "chart files (*.chart)|*.chart";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                fileName = saveDialog.FileName;
            }
            else
                throw new System.Exception("File was not saved");
#endif
            Save(fileName);
            lastLoadedFile = fileName;
        }
        catch (System.Exception e)
        {
            // User probably just canceled
            Debug.LogError(e.Message);
        }
    }

    void Save (string filename)
    {
        if (currentSong != null)
            currentSong.Save(filename);
    }

    public void Play()
    {
        float strikelinePos = strikeline.position.y;
        musicSource.time = Song.WorldYPositionToTime(strikelinePos) + currentSong.offset;       // No need to add audio calibration as position is base on the strikeline position
        play.interactable = false;
        Globals.applicationMode = Globals.ApplicationMode.Playing;
        musicSource.Play();
    }

    public void Stop()
    {
        play.interactable = true;
        Globals.applicationMode = Globals.ApplicationMode.Editor;
        musicSource.Stop();
    }

    IEnumerator _LoadSong()
    {
        Song backup = currentSong;
#if TIMING_DEBUG
        float totalLoadTime = 0;
#endif
        try
        {
            openFileDialog.filter = "Chart files\0*.chart";
#if UNITY_EDITOR
            currentFileName = UnityEditor.EditorUtility.OpenFilePanel("Load Chart", "", "chart");
#else
            if (LibWrap.GetOpenFileName(openFileDialog))
            {
                currentFileName = openFileDialog.file;
            }
            else
            {
                throw new System.Exception("Could not open file");
            }
#endif
#if TIMING_DEBUG
            totalLoadTime = Time.realtimeSinceStartup;
#endif
            while (currentSong.IsSaving) ;
            currentSong = new Song(currentFileName);
#if TIMING_DEBUG
            Debug.Log("Song load time: " + (Time.realtimeSinceStartup - totalLoadTime));

            float objectDestroyTime = Time.realtimeSinceStartup;
#endif
            // Remove objects from previous chart
            foreach (Transform chartObject in chartObjectParent.transform)
            {
                Destroy(chartObject.gameObject);
            }
            foreach (Transform songObject in songObjectParent.transform)
            {
                Destroy(songObject.gameObject);
            }
            foreach (Transform child in guiIndicators.transform)
            {
                Destroy(child.gameObject);
            }
#if TIMING_DEBUG
            Debug.Log("Chart objects destroy time: " + (Time.realtimeSinceStartup - objectDestroyTime));
#endif
            currentChart = currentSong.expert_single;
#if TIMING_DEBUG
            float objectLoadTime = Time.realtimeSinceStartup;
#endif
            // Create the actual objects
            CreateSongObjects(currentSong);
            CreateChartObjects(currentChart);

#if TIMING_DEBUG
            Debug.Log("Chart objects load time: " + (Time.realtimeSinceStartup - objectLoadTime));
#endif
            songNameText.text = currentSong.name;

            lastLoadedFile = currentFileName;
        }
        catch (System.Exception e)
        {
            // Most likely closed the window explorer, just ignore for now.
            currentSong = backup;
            Debug.LogError(e.Message);

            yield break;
        }

        while (currentSong.musicStream != null && currentSong.musicStream.loadState != AudioDataLoadState.Loaded)
        {
            Debug.Log("Loading audio...");
            yield return null;
        }

        if (currentSong.musicStream != null)
        {
            musicSource.clip = currentSong.musicStream;
            movement.SetPosition(0);
        }
#if TIMING_DEBUG
        Debug.Log("Total load time: " + (Time.realtimeSinceStartup - totalLoadTime));
#endif
    }

    // Create Sections, bpms, events and time signature objects
    GameObject CreateSongObjects(Song song)
    {
        for (int i = 0; i < song.sections.Length; ++i)
        {
            // Convert the chart data into gameobject
            GameObject sectionObject = Instantiate(this.section);

            sectionObject.transform.SetParent(songObjectParent.transform);
            
            // Attach the note to the object
            SectionController controller = sectionObject.GetComponentInChildren<SectionController>();

            // Link controller and note together
            controller.Init(song.sections[i], timeHandler, guiIndicators);
            
            controller.UpdateSongObject();
            
        }
        
        return songObjectParent;
    }

    // Create note, starpower and chart event objects
    GameObject CreateChartObjects(Chart chart)
    {    
        // Get reference to the current set of notes in case real notes get deleted
        Note[] notes = chart.notes;
        for (int i = 0; i < notes.Length; ++i)
        {
            // Make sure notes haven't been deleted
            if (notes[i].song != null)
            {
                NoteController controller = CreateNoteObject(notes[i], chartObjectParent);
                controller.UpdateSongObject();
            }
        }

        return chartObjectParent;
    }

    public NoteController CreateNoteObject(Note note, GameObject parent = null)
    {
        // Convert the chart data into gameobject
        GameObject noteObject = Instantiate(this.note);

        if (parent)
            noteObject.transform.SetParent(parent.transform);
        else
            noteObject.transform.SetParent(chartObjectParent.transform);

        // Attach the note to the object
        NoteController controller = noteObject.GetComponent<NoteController>();

        // Link controller and note together
        controller.Init(note);

        return controller;
    }
}