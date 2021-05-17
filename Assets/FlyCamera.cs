using System.Runtime.InteropServices;
using UnityEngine;
//Logic of camera movements
public class FlyCamera : MonoBehaviour
{
  [DllImport("user32.dll")]
  static extern bool SetCursorPos(int X, int Y);
  bool over_UI = false;
  public float mainSpeed;// = 25; //regular speed
  public float shiftAdd;// = 50; //multiplied by how long shift is held.  Basically running
  public float maxShift;// = 100 //Maximum speed when holdin gshift
  public float camSens;// = 0.25f; //How sensitive it with mouse
  private Vector3 lastMouse = new Vector3(0, 0, 0); //kind of in the middle of the screen, rather than at the top (play)
  private float totalRun = 1.0f;
  private int flag = 0;
  int Rotation = 0;
  /// <summary>
  /// Whether cam is standard or birds-eye
  /// </summary>
  public static bool isStandardCam = true;
  float Ordinary_cam_last_height = 20f;
  public GameObject SaveMenu;

  void Update()
  {
    if (!SaveMenu.activeSelf)
    {
      if (Input.GetKeyUp(KeyCode.BackQuote))
      {
        float posY = this.transform.position.y;
        Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(r.origin, r.direction, out RaycastHit hit, Mathf.Infinity, 1 << 8))
        { // if casting elevated element from underground, raise camera up to it
          this.transform.position = new Vector3(hit.transform.position.x, 
            posY > hit.transform.position.y ? posY : hit.transform.position.y + 10, hit.transform.position.z);
        }
      }
      over_UI = (Input.mousePosition.x < 0.232 * Screen.width) ? true : false;
      if (Input.GetKeyDown(KeyCode.PageUp))
      {
        Vector3 pos = transform.position;
        pos.y += 80f;
        transform.position = pos;
      }
      if (Input.GetKeyDown(KeyCode.PageDown))
      {
        Vector3 pos = transform.position;
        pos.y -= 80f;
        transform.position = pos;
      }
      if (Input.GetKeyDown(KeyCode.Home))
      {
        transform.position = new Vector3(5, 20, 5);
        transform.rotation = (isStandardCam) ? Quaternion.Euler(45, 45, 0) : Quaternion.Euler(90, 0, 0);
      }
      if (Input.GetKeyDown("enter")) // switch camera type
      {
        isStandardCam = !isStandardCam;
        if (!isStandardCam)
        { // switch to birds eye
          Ordinary_cam_last_height = transform.position.y;
          //  
          transform.position = new Vector3(transform.position.x, Service.maxHeight, transform.position.z);
        }
        else
        { //switch to ordinary
          transform.position = new Vector3(transform.position.x, Ordinary_cam_last_height, transform.position.z);
        }
      }

      if (isStandardCam && !SaveMenu.activeSelf)
        Ordinarycamera();
      else
        birdseyecamera();
    }
  }

  void birdseyecamera()
  {
    GetComponent<Camera>().orthographic = true;
    transform.rotation = Quaternion.Euler(new Vector3(90, Rotation, 0));
    if (Input.GetKey(KeyCode.PageDown))
      GetComponent<Camera>().orthographicSize += 1;
    else if (Input.GetKey(KeyCode.PageUp))
    {
      if (GetComponent<Camera>().orthographicSize - 3 > 0)
        GetComponent<Camera>().orthographicSize -= 1;
    }
    if (Input.GetKeyUp(KeyCode.F1))
      Rotation = 45;
    else if (Input.GetKeyUp(KeyCode.F2))
      Rotation = 0;
    else if (Input.GetKeyUp(KeyCode.F3))
      Rotation = -45;

    //o
    Vector3 p = GetBaseInput();

    transform.Translate(p, Space.World);
  }
  void Ordinarycamera()
  {
    GetComponent<Camera>().orthographic = false;

    if (Input.GetKey(KeyCode.Space))
    {
      //Debug.Log ("input:"+Input.mousePosition);
      //Debug.Log ("last:"+lastMouse);
      if (flag == 0)
      {
        lastMouse = Input.mousePosition - lastMouse;
      }
      else
      {
        lastMouse.x = 0;
        lastMouse.y = 0;
        lastMouse.z = 0;
        flag = 0;
      }
      lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0); //Obrót bieżący
      lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x, transform.eulerAngles.y + lastMouse.y, 0);
      transform.eulerAngles = lastMouse;
      if (Input.mousePosition.x < 0.232 * Screen.width || Input.mousePosition.x > Screen.width - 10 || Input.mousePosition.y < 0.094 * Screen.height || Input.mousePosition.y > Screen.height - 10)
      {
        SetCursorPos(Mathf.RoundToInt(Screen.width / 2f), Mathf.RoundToInt((Screen.height / 2f)));
        flag = 1;
      }

    }
    lastMouse = Input.mousePosition;
    //Ruch
    Vector3 p = GetBaseInput();
    if (Input.GetKey(KeyCode.LeftShift))
    {
      //totalRun += Time.deltaTime;
      p = p * shiftAdd;
      p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
      p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
      p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
    }
    else
    {
      totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
      p = p * mainSpeed;
    }

    //Lock to XZ plane
    p = p * Time.deltaTime;
    Vector3 newPosition = transform.position;
    //		if (Input.GetKey(KeyCode.LeftControl))
    //		{ 
    //            transform.Translate(p);
    //            newPosition.x = transform.position.x;
    //            newPosition.z = transform.position.z;
    //            transform.position = newPosition;
    //        }
    //        else
    //		{
    transform.Translate(p);
    //        }
  }
  private Vector3 GetBaseInput()
  {
    Vector3 p_Velocity = new Vector3();
    if (Input.GetKey(KeyCode.W))
    {
      p_Velocity += new Vector3(0, 0, 1);
    }
    if (Input.GetKey(KeyCode.S))
    {
      p_Velocity += new Vector3(0, 0, -1);
    }
    if (Input.GetKey(KeyCode.A))
    {
      p_Velocity += new Vector3(-1, 0, 0);
    }
    if (Input.GetKey(KeyCode.D))
    {
      p_Velocity += new Vector3(1, 0, 0);
    }
    return p_Velocity;
  }
}