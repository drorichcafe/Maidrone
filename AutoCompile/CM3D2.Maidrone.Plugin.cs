﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2.Maidrone
{
	[PluginFilter("CM3D2x64"), PluginFilter("CM3D2x86"), PluginName("Maidrone"), PluginVersion("0.0.0.7")]
	public class Maidrone : PluginBase
	{
		public class Waypoint
		{
			public Vector3 Position = Vector3.zero;
			public Vector3 Rotation = Vector3.zero;
		}

		public class ManualInfo
		{
			public Vector3 Position = Vector3.zero;
			public float RotSpeed = 15.0f;
			public float AccelXZ = 0.2f;
			public float AccelY = 0.2f;
			public float Brake = 0.1f;
		}

		public class WaypointInfo
		{
			public float MoveSpeed = 1.0f;
			public List<Waypoint> Waypoints = new List<Waypoint>();
		}

		public class LissajousCurve
		{
			public Vector3 Position = Vector3.zero;
			public Vector3 Amplitude = new Vector3(1.0f, 0.1f, 1.0f);
			public Vector3 Frequency = new Vector3(1.0f, 50.0f, 1.0f);
			public Vector3 Offset = new Vector3(0.0f, 0.0f, 180.0f);
		}

		public class LissajousInfo
		{
			public bool FocusMaid = true;
			public string FocusTransform = "Bip01 Spine1";
			public Vector3 DefaultPosition = Vector3.zero;
			public LissajousCurve Lissajous = new LissajousCurve();
		}

		public class ScreenSaverInfo
		{
			public bool Enable = true;
			public float Time = 60.0f;
			public Drone.Algorithm Algorithm = Drone.Algorithm.Lissajous;
			public bool FirstPersonView = false;
		}

		public class Config
		{
			public KeyCode KbBoot = KeyCode.None;
			public KeyCode KbPrintInfo = KeyCode.None;
			public KeyCode KbSwitchCamera = KeyCode.None;
			public KeyCode KbSwitchAlgorithm = KeyCode.None;
			public KeyCode KbToggleFocus = KeyCode.None;
			public KeyCode KbFindPrev = KeyCode.None;
			public KeyCode KbFindNext = KeyCode.None;
			public KeyCode KbResetPosition = KeyCode.None;
			public KeyCode KbMoveStop = KeyCode.None;
			public KeyCode KbMoveForward = KeyCode.None;
			public KeyCode KbMoveBackward = KeyCode.None;
			public KeyCode KbMoveLeft = KeyCode.None;
			public KeyCode KbMoveRight = KeyCode.None;
			public KeyCode KbMoveUp = KeyCode.None;
			public KeyCode KbMoveDown = KeyCode.None;
			public KeyCode KbRotateUp = KeyCode.None;
			public KeyCode KbRotateDown = KeyCode.None;
			public KeyCode KbRotateLeft = KeyCode.None;
			public KeyCode KbRotateRight = KeyCode.None;

			public KeyCode JsBoot = KeyCode.None;
			public KeyCode JsPrintInfo = KeyCode.None;
			public KeyCode JsSwitchCamera = KeyCode.None;
			public KeyCode JsSwitchAlgorithm = KeyCode.None;
			public KeyCode JsToggleFocus = KeyCode.None;
			public KeyCode JsFindPrev = KeyCode.None;
			public KeyCode JsFindNext = KeyCode.None;
			public KeyCode JsResetPosition = KeyCode.None;
			public KeyCode JsMoveStop = KeyCode.None;
			public string JsMoveX = string.Empty;
			public string JsMoveY = string.Empty;
			public string JsMoveZ = string.Empty;
			public string JsRotateX = string.Empty;
			public string JsRotateY = string.Empty;			
			public bool JsFlipMoveX = false;
			public bool JsFlipMoveY = false;
			public bool JsFlipMoveZ = false;
			public bool JsFlipRotateX = false;
			public bool JsFlipRotateY = false;
			
			public float CameraPitchSpeed = 15.0f;
			public float CameraDistance = 0.25f;
			public float CameraFovFp = 45.0f;
			public float CameraFovTp = 45.0f;
			public float ModelScale = 0.05f;
			public LissajousCurve ModelLissajous = new LissajousCurve();
			public ManualInfo ManualSetting = new ManualInfo();
			public WaypointInfo WaypointSetting = new WaypointInfo();
			public LissajousInfo LissajousSetting = new LissajousInfo();
			public ScreenSaverInfo ScreenSaverSetting = new ScreenSaverInfo();
		}

		public struct InputValue
		{
			public bool boot;
			public bool printInfo;
			public bool switchCamera;
			public bool switchAlgorithm;
			public bool toggleFocus;
			public bool findPrev;
			public bool findNext;
			public bool resetPosition;
			public bool moveStop;
			public Vector3 move;
			public Vector3 rotate;
		}

		class GameState
		{
			public Vector3 cameraPosition = new Vector3();
			public Quaternion cameraRotation = new Quaternion();
			public float cameraFov = 45.0f;
			public bool droneIsCreated = false;
			public float cameraPitch = 0.0f;
			public bool firstPersonView = false;
			public Drone.Algorithm algorithm = Drone.Algorithm.Manual;
			public Vector3 dronePosition = new Vector3();
			public Quaternion droneRotation = new Quaternion();
		}

		static Config config = new Config();
		static bool isAppFocused = true;
		static bool isScreenSaver = false;
		static GameState originalState = new GameState();
		static Vector3 mousePosition = new Vector3();
		static float ssInvokeTimer = 0.0f;
		static int level = 0;

		public static InputValue getInputValue()
		{
			InputValue ret;
			ret.boot = Input.GetKeyDown(config.KbBoot) || Input.GetKeyDown(config.JsBoot);
			ret.printInfo = Input.GetKeyDown(config.KbPrintInfo) || Input.GetKeyDown(config.JsPrintInfo);
			ret.switchCamera = Input.GetKeyDown(config.KbSwitchCamera) || Input.GetKeyDown(config.JsSwitchCamera);
			ret.switchAlgorithm = Input.GetKeyDown(config.KbSwitchAlgorithm) || Input.GetKeyDown(config.JsSwitchAlgorithm);
			ret.toggleFocus = Input.GetKeyDown(config.KbToggleFocus) || Input.GetKeyDown(config.JsToggleFocus);
			ret.findPrev = Input.GetKeyDown(config.KbFindPrev) || Input.GetKeyDown(config.JsFindPrev);
			ret.findNext = Input.GetKeyDown(config.KbFindNext) || Input.GetKeyDown(config.JsFindNext);
			ret.resetPosition = Input.GetKeyDown(config.KbResetPosition) || Input.GetKeyDown(config.JsResetPosition);
			ret.moveStop = Input.GetKeyDown(config.KbMoveStop) || Input.GetKeyDown(config.JsMoveStop);
			ret.move = Vector3.zero;
			ret.rotate = Vector3.zero;

			if (config.JsMoveX != string.Empty) ret.move.x = Input.GetAxis(config.JsMoveX);
			if (config.JsMoveY != string.Empty) ret.move.y = Input.GetAxis(config.JsMoveY);
			if (config.JsMoveZ != string.Empty) ret.move.z = Input.GetAxis(config.JsMoveZ);
			if (config.JsRotateX != string.Empty) ret.rotate.x = Input.GetAxis(config.JsRotateX);
			if (config.JsRotateY != string.Empty) ret.rotate.y = Input.GetAxis(config.JsRotateY);
			if (config.JsFlipMoveX) ret.move.x *= -1;
			if (config.JsFlipMoveY) ret.move.y *= -1;
			if (config.JsFlipMoveZ) ret.move.z *= -1;
			if (config.JsFlipRotateX) ret.rotate.x *= -1;
			if (config.JsFlipRotateY) ret.rotate.y *= -1;

			if (ret.move.x * ret.move.x < 0.2f * 0.2f) ret.move.x = 0.0f;
			if (ret.move.y * ret.move.y < 0.2f * 0.2f) ret.move.y = 0.0f;
			if (ret.move.z * ret.move.z < 0.2f * 0.2f) ret.move.z = 0.0f;
			if (ret.rotate.x * ret.rotate.x < 0.2f * 0.2f) ret.rotate.x = 0.0f;
			if (ret.rotate.y * ret.rotate.y < 0.2f * 0.2f) ret.rotate.y = 0.0f;

			if (Input.GetKey(config.KbMoveUp)) ret.move.y = 1.0f;
			if (Input.GetKey(config.KbMoveDown)) ret.move.y = -1.0f;
			if (Input.GetKey(config.KbMoveForward)) ret.move.z = 1.0f;
			if (Input.GetKey(config.KbMoveBackward)) ret.move.z = -1.0f;
			if (Input.GetKey(config.KbMoveRight)) ret.move.x = 1.0f;
			if (Input.GetKey(config.KbMoveLeft)) ret.move.x = -1.0f;
			if (Input.GetKey(config.KbRotateRight)) ret.rotate.y = 1.0f;
			if (Input.GetKey(config.KbRotateLeft)) ret.rotate.y = -1.0f;
			if (Input.GetKey(config.KbRotateUp)) ret.rotate.x = 1.0f;
			if (Input.GetKey(config.KbRotateDown)) ret.rotate.x = -1.0f;

			return ret;
		}

		public void Awake()
		{
			loadConfig();
			originalState.dronePosition = config.ManualSetting.Position;
			ssInvokeTimer = Time.time;
			DontDestroyOnLoad(this);
		}

		public void OnLevelWasLoaded(int lv)
		{
			level = lv;
		}

		public void OnApplicationFocus(bool focusStatus)
		{
			isAppFocused = focusStatus;
			ssInvokeTimer = Time.time;
			mousePosition = Input.mousePosition;

			if (isScreenSaver)
			{
				endScreenSaver();
			}
		}

		public void LateUpdate()
		{
			var iv = getInputValue();

			if (isScreenSaver)
			{
				if (isAppFocused && (Input.anyKey || Input.mousePosition != mousePosition))
				{
					endScreenSaver();
				}
			}
			else {
				if (isAppFocused && iv.boot)
				{
					var go = GameObject.Find("Maidrone");
					if (go == null)
					{
						loadConfig();
						
						go = new GameObject("Maidrone");
						go.AddComponent<Drone>();

						saveCameraState();
						loadDroneState();
					}
					else
					{
						saveDroneState();
						loadCameraState();

						Destroy(go);
					}
				}
				else if (isAppFocused && (Input.anyKey || Input.mousePosition != mousePosition))
				{
					ssInvokeTimer = Time.time;
					mousePosition = Input.mousePosition;
				}
				else if(config.ScreenSaverSetting.Enable)
				{
					bool allowChangeToSS = true;

					if (level == 26)
					{
						allowChangeToSS = false;
					}
					else
					{
						var go = GameObject.Find("Maidrone");
						if (go != null)
						{
							var dr = go.GetComponent<Drone>();
							if (dr != null)
							{
								allowChangeToSS = (dr.algorithm == Drone.Algorithm.Manual);
							}
						}

						if (allowChangeToSS && Time.time - ssInvokeTimer > config.ScreenSaverSetting.Time)
						{
							beginScreenSaver();
						}
					}
				}
			}
		}

		private void beginScreenSaver()
		{
			Console.WriteLine("Maidrone: スクリーンセーバーを起動");

			loadConfig();

			var go = GameObject.Find("Maidrone");
			originalState.droneIsCreated = go != null;
			if (go == null) go = new GameObject("Maidrone");
			else saveDroneState();
			saveCameraState();

			var dr = go.GetComponent<Drone>();
			if (dr == null) dr = go.AddComponent<Drone>();
			dr.changeAlgorithm(config.ScreenSaverSetting.Algorithm);
			dr.changeView(config.ScreenSaverSetting.FirstPersonView);

			ssInvokeTimer = Time.time;
			mousePosition = Input.mousePosition;
			isScreenSaver = true;
		}

		private void endScreenSaver()
		{
			Console.WriteLine("Maidrone: スクリーンセーバーを終了");

			if (!originalState.droneIsCreated)
			{
				var go = GameObject.Find("Maidrone");
				if (go != null) Destroy(go);
			}
			else
			{
				loadDroneState();
			}

			loadCameraState();

			ssInvokeTimer = Time.time;
			mousePosition = Input.mousePosition;
			isScreenSaver = false;
		}

		private void saveCameraState()
		{
			var cam = GameObject.Find("CameraMain");
			if (cam != null)
			{
				originalState.cameraPosition = cam.transform.position;
				originalState.cameraRotation = cam.transform.rotation;
				originalState.cameraFov = cam.GetComponent<Camera>().fieldOfView;
			}
		}

		private void loadCameraState()
		{
			var cam = GameObject.Find("CameraMain");
			if (cam != null)
			{
				cam.transform.position = originalState.cameraPosition;
				cam.transform.rotation = originalState.cameraRotation;
				cam.GetComponent<Camera>().fieldOfView = originalState.cameraFov;
			}
		}

		private void saveDroneState()
		{
			var go = GameObject.Find("Maidrone");
			if (go != null)
			{
				var dr = go.GetComponent<Drone>();
				if (dr != null)
				{
					originalState.algorithm = dr.algorithm;
					originalState.firstPersonView = dr.firstPersonView;
					originalState.cameraPitch = dr.cameraPitch;
					originalState.dronePosition = go.transform.position;
					originalState.droneRotation = go.transform.rotation;
				}
			}
		}

		private void loadDroneState()
		{
			var go = GameObject.Find("Maidrone");
			if (go != null)
			{
				var dr = go.GetComponent<Drone>();
				if (dr != null)
				{
					dr.changeAlgorithm(originalState.algorithm);
					dr.changeView(originalState.firstPersonView);
					dr.cameraPitch = originalState.cameraPitch;
					go.transform.position = originalState.dronePosition;
					go.transform.rotation = originalState.droneRotation;
				}
			}
		}

		private void loadConfig()
		{
			System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Config));

			var fname = Path.Combine(this.DataPath, "Maidrone.xml");
			if (!File.Exists(fname))
			{
				using (StreamWriter sw = new StreamWriter(fname, false, new UTF8Encoding(true)))
				{
					 serializer.Serialize(sw, config);
				}
			}
			else
			{
				using (StreamReader sr = new StreamReader(fname, new UTF8Encoding(true)))
				{
					config = (Config)serializer.Deserialize(sr);
				}
			}
		}

		public class Drone : MonoBehaviour
		{
			public enum Algorithm
			{
				Manual,
				Waypoint,
				Lissajous,
			}

			private float m_cameraPitch = 0.0f;
			private Vector3 m_velocity = Vector3.zero;
			private bool m_firstPersonView = false;
			private Algorithm m_alg = Algorithm.Manual;
			private int m_focusMaid = 0;
			private bool m_autoFocus = true;
			private Vector3 m_lsgCenter = Vector3.zero;
			private Vector3 m_lsgOffsetH = Vector3.zero;
			private float m_waypointDepartureTime = 0.0f;
			private float m_waypointEstimateTime = 0.0f;
			private int m_prevWaypoint = 0;
			private int m_nextWaypoint = 0;
			private GameObject m_model = null;
			private GameObject m_blade = null;

			public float cameraPitch { get { return m_cameraPitch; } set { m_cameraPitch = value; } }
			public bool firstPersonView { get { return m_firstPersonView; } }
			public Algorithm algorithm { get { return m_alg; } }

			void Start()
			{
				var model = new GameObject();
				model.transform.parent = this.gameObject.transform;
				model.transform.localPosition = Vector3.zero;
				model.transform.localScale = new Vector3(config.ModelScale, config.ModelScale, config.ModelScale);
				var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				body.transform.parent = model.gameObject.transform;
				body.transform.localPosition = Vector3.zero;
				body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
				var poll = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				poll.transform.parent = model.gameObject.transform;
				poll.transform.localPosition = new Vector3(0f, 0.25f, 0.0f);
				poll.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
				var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
				blade.transform.parent = model.gameObject.transform;
				blade.transform.localPosition = new Vector3(0f, 0.3f, 0.0f);
				blade.transform.localScale = new Vector3(0.5f, 0.01f, 0.1f);

				rb.mass = 10.0f;
				rb.isKinematic = true;

				m_model = model;
				m_blade = blade;

				m_lsgCenter = config.LissajousSetting.DefaultPosition;
				m_lsgOffsetH = Vector3.zero;
				m_autoFocus = config.LissajousSetting.FocusMaid;

				changeView(m_firstPersonView);
			}

			void Update()
			{
				var iv = getInputValue();
				var config = Maidrone.config;

				if (iv.printInfo)
				{
					Console.WriteLine("Maidrone: Position " + transform.position.ToString());
					Console.WriteLine("Maidrone: Rotation " + transform.rotation.ToString());
					if (m_alg == Algorithm.Lissajous)
					{
						Console.WriteLine("Maidrone: LissajousCenter " + m_lsgCenter.ToString());
						Console.WriteLine("Maidrone: LissajousOffset " + (m_lsgOffsetH + config.LissajousSetting.Lissajous.Position).ToString());
						Console.WriteLine("Maidrone: LissajousAmplitude " + config.LissajousSetting.Lissajous.Amplitude.ToString());
					}
				}

				if (iv.switchAlgorithm)
				{
					switch (m_alg)
					{
						case Algorithm.Manual:
							changeAlgorithm(Algorithm.Waypoint);
							break;
						case Algorithm.Waypoint:
							changeAlgorithm(Algorithm.Lissajous);
							break;
						case Algorithm.Lissajous:
							changeAlgorithm(Algorithm.Manual);
							break;
					}
				}

				if (m_alg == Algorithm.Manual)
				{
					var man = config.ManualSetting;

					float accXZ = man.AccelXZ * Time.deltaTime;
					float accY = man.AccelY * Time.deltaTime;

					// control player
					if (iv.resetPosition)
					{
						transform.position = man.Position;
						transform.rotation = new Quaternion();
						m_cameraPitch = 0.0f;
						m_velocity = new Vector3();
					}

					transform.rotation = Quaternion.Euler(0.0f, man.RotSpeed * Time.deltaTime * iv.rotate.y, 0.0f) * transform.rotation;
					m_velocity += transform.right * (accXZ * iv.move.x);
					m_velocity += transform.up * (accY * iv.move.y);
					m_velocity += transform.forward * (accXZ * iv.move.z);

					if (iv.moveStop)
					{
						m_velocity = Vector3.zero;
					}

					transform.position += m_velocity * Time.deltaTime;
					m_velocity -= m_velocity * Mathf.Clamp(Time.deltaTime * man.Brake, 0.0f, 1.0f);

					controlCamera();
				}
				else if (m_alg == Algorithm.Waypoint)
				{
					float t = Time.time - m_waypointDepartureTime;
					while (t > m_waypointEstimateTime)
					{
						t -= m_waypointEstimateTime;
						gotoNextWaypoint();
					}

					var wp = config.WaypointSetting.Waypoints;
					var pos = Vector3.Lerp(wp[m_prevWaypoint].Position, wp[m_nextWaypoint].Position, t / m_waypointEstimateTime);
					transform.position = pos;
					var rot = Vector3.Lerp(wp[m_prevWaypoint].Rotation, wp[m_nextWaypoint].Rotation, t / m_waypointEstimateTime);
					transform.rotation = Quaternion.Euler(0.0f, rot.y, 0.0f);
					m_cameraPitch = rot.x;
				}
				else if (m_alg == Algorithm.Lissajous)
				{
					var lss = config.LissajousSetting;
					var lsg = lss.Lissajous;
					
					m_lsgOffsetH += iv.move.y * transform.up * 0.5f * Time.deltaTime;
					
					bool oldAutoFocus = m_autoFocus;

					if (iv.toggleFocus)
					{
						m_autoFocus = !m_autoFocus;
					}

					if (iv.findPrev)
					{
						var maid = getPrevMaid();
						if (maid != null) m_lsgCenter = focusMaidTransform(maid, lss.FocusTransform);
						else m_autoFocus = false;
					}
					else if (iv.findNext)
					{
						var maid = getNextMaid();
						if (maid != null) m_lsgCenter = focusMaidTransform(maid, lss.FocusTransform);
						else m_autoFocus = false;
					}
					else if (m_autoFocus)
					{
						var maid = getCurrentMaid();
						if (maid != null)
						{
							m_lsgCenter = focusMaidTransform(maid, lss.FocusTransform);
							lsg.Amplitude -= new Vector3(0.5f, 0.0f, 0.5f) * iv.move.z * Time.deltaTime;
						}
						else m_autoFocus = false;
					}
					else
					{
						m_lsgCenter += iv.move.z * transform.forward * Time.deltaTime;
						m_lsgCenter += iv.move.x * transform.right * Time.deltaTime;
					}

					if (oldAutoFocus != m_autoFocus)
					{
						if (m_autoFocus) Console.WriteLine("Maidrone: Enable Maid Focus");
						else Console.WriteLine("Maidrone: Disable Maid Focus");
					}

					Vector3 center = m_lsgCenter + m_lsgOffsetH + lsg.Position;
					transform.position = new Vector3(
						center.x + Mathf.Sin(2.0f * Mathf.PI * lsg.Frequency.x * Time.time + Mathf.Deg2Rad * lsg.Offset.x) * lsg.Amplitude.x,
						center.y + Mathf.Sin(2.0f * Mathf.PI * lsg.Frequency.y * Time.time + Mathf.Deg2Rad * lsg.Offset.y) * lsg.Amplitude.y,
						center.z + Mathf.Sin(2.0f * Mathf.PI * lsg.Frequency.z * Time.time + Mathf.Deg2Rad * lsg.Offset.z) * lsg.Amplitude.z);
					transform.LookAt(center);

					controlCamera();
				}

				// animation
				if (m_blade != null)
				{
					m_blade.transform.rotation = Quaternion.Euler(0.0f, 1440.0f * Time.deltaTime, 0.0f) * m_blade.transform.rotation;
				}

				if (m_model != null)
				{
					var ml = config.ModelLissajous;
					m_model.transform.localPosition = new Vector3(
						ml.Position.x + Mathf.Sin(2.0f * Mathf.PI * ml.Frequency.x * Time.time + Mathf.Deg2Rad * ml.Offset.x) * ml.Amplitude.x,
						ml.Position.y + Mathf.Sin(2.0f * Mathf.PI * ml.Frequency.y * Time.time + Mathf.Deg2Rad * ml.Offset.y) * ml.Amplitude.y,
						ml.Position.z + Mathf.Sin(2.0f * Mathf.PI * ml.Frequency.z * Time.time + Mathf.Deg2Rad * ml.Offset.z) * ml.Amplitude.z);
				}

				if (iv.switchCamera)
				{
					changeView(!m_firstPersonView);
				}
			}

			public void LateUpdate()
			{
				// overwrite scene camera
				var cam = GameObject.Find("CameraMain");
				if (cam != null)
				{
					if (false == m_firstPersonView)
					{
						cam.transform.position = transform.position - Quaternion.AngleAxis(m_cameraPitch, transform.right) * (transform.forward * config.CameraDistance);
						cam.transform.LookAt(transform.position);
						cam.GetComponent<Camera>().fieldOfView = config.CameraFovTp;
					}
					else
					{
						cam.transform.position = transform.position;
						cam.transform.LookAt(transform.position + Quaternion.AngleAxis(m_cameraPitch, transform.right) * transform.forward);
						cam.GetComponent<Camera>().fieldOfView = config.CameraFovFp;
					}
				}
			}

			public void changeAlgorithm(Algorithm alg)
			{
				m_alg = alg;

				switch (m_alg)
				{
					case Algorithm.Manual:
						break;

					case Algorithm.Waypoint:
						if (config.WaypointSetting.Waypoints.Count == 0)
						{
							Console.WriteLine("Error: Maidrone - Waypointが見つかりませんでした。Lissajousに切り替えます。");
							m_alg = Algorithm.Lissajous;
						}
						else
						{
							var wp = config.WaypointSetting.Waypoints;
							transform.position = wp[0].Position;
							transform.rotation = Quaternion.Euler(0.0f, wp[0].Rotation.y, 0.0f);
							m_cameraPitch = wp[0].Rotation.x;
							m_prevWaypoint = 0;
							m_nextWaypoint = 0;
							gotoNextWaypoint();
						}
						break;
					case Algorithm.Lissajous:
						break;
				}
			}

			private Vector3 focusMaidTransform(GameObject maidGo, string name)
			{
				var ts = maidGo.transform.GetComponentsInChildren<Transform>();
				if (ts.Length == 0) return new Vector3(0.0f, 1.0f, 0.0f);

				foreach (var t in ts)
				{
					if (t.gameObject.name == name)
					{
						return t.position;
					}
				}

				Console.WriteLine("Error: Maidrone - Cannot found maid transform [" + name + "] in " + maidGo.name);
				return Vector3.zero;
			}

			private GameObject getCurrentMaid()
			{
				var cm = GameMain.Instance.CharacterMgr;
				var cnt = cm.GetMaidCount();
				if (cnt == 0) return null;

				var m = cm.GetMaid(m_focusMaid);
				if (m == null || m.gameObject == null) return null;
				if (m.gameObject.transform.GetComponentsInChildren<Transform>().Length == 0) return null;
				return m.gameObject;
			}

			private GameObject getPrevMaid()
			{
				var cm = GameMain.Instance.CharacterMgr;
				var cnt = cm.GetMaidCount();
				if (cnt == 0) return null;

				int old = m_focusMaid;
				do
				{
					m_focusMaid--;
					if (m_focusMaid < 0) m_focusMaid = cnt - 1;
					if (m_focusMaid >= cnt) m_focusMaid = 0;
				} while (cm.GetMaid(m_focusMaid) == null && m_focusMaid != old);

				var m = cm.GetMaid(m_focusMaid);
				if (m == null || m.gameObject == null) return null;
				if (m.gameObject.transform.GetComponentsInChildren<Transform>().Length == 0) return null;
				return m.gameObject;
			}

			private GameObject getNextMaid()
			{
				var cm = GameMain.Instance.CharacterMgr;
				var cnt = cm.GetMaidCount();
				if (cnt == 0) return null;

				int old = m_focusMaid;
				do
				{
					m_focusMaid++;
					if (m_focusMaid < 0) m_focusMaid = cnt - 1;
					if (m_focusMaid >= cnt) m_focusMaid = 0;
				} while (cm.GetMaid(m_focusMaid) == null && m_focusMaid != old);

				var m = cm.GetMaid(m_focusMaid);
				if (m == null || m.gameObject == null) return null;
				if (m.gameObject.transform.GetComponentsInChildren<Transform>().Length == 0) return null;
				return m.gameObject;
			}

			public void changeView(bool fpv)
			{
				m_firstPersonView = fpv;

				if (m_model != null)
				{
					for (int i = 0; i < m_model.transform.childCount; i++)
					{
						m_model.transform.GetChild(i).renderer.enabled = !m_firstPersonView;
					}
				}
			}

			private void gotoNextWaypoint()
			{
				m_prevWaypoint = m_nextWaypoint;
				m_nextWaypoint = m_nextWaypoint + 1;
				m_waypointDepartureTime = Time.time;
				
				var wp = config.WaypointSetting.Waypoints;
				if (m_nextWaypoint >= wp.Count)
				{
					m_prevWaypoint = 0;
					m_nextWaypoint = 1;
				}

				m_waypointEstimateTime = (wp[m_prevWaypoint].Position - wp[m_nextWaypoint].Position).magnitude / config.WaypointSetting.MoveSpeed;
				if (m_waypointEstimateTime < 0.1f) m_waypointEstimateTime = 0.1f;
			}

			private void controlCamera()
			{
				var iv = getInputValue();
				m_cameraPitch += iv.rotate.x * config.CameraPitchSpeed * Time.deltaTime;
				m_cameraPitch = Mathf.Clamp(m_cameraPitch, -75.0f, 75.0f);
			}

			
		}
	}
}