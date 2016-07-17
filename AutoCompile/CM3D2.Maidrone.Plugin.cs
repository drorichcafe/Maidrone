using System;
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
	[PluginFilter("CM3D2x64"), PluginFilter("CM3D2x86"), PluginName("Maidrone"), PluginVersion("0.0.0.1")]
	public class Maidrone : PluginBase
	{
		public class Waypoint
		{
			public Vector3 Position = new Vector3(0.0f, 0.0f, 0.0f);
			public Vector3 Rotation = new Vector3(0.0f, 0.0f, 0.0f);
		}

		public class ManualInfo
		{
			public KeyCode RotateLeft = KeyCode.LeftArrow;
			public KeyCode RotateRight = KeyCode.RightArrow;
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

		public class LissajousInfo
		{
			public Vector3 Position = new Vector3(0.0f, 0.0f, 0.0f);
			public Vector3 Amplitude = new Vector3(1.0f, 0.1f, 1.0f);
			public Vector3 Frequency = new Vector3(1.0f, 50.0f, 1.0f);
			public Vector3 Offset = new Vector3(0.0f, 0.0f, 180.0f);
		}

		public class Config
		{
			public KeyCode Boot = KeyCode.F11;
			public KeyCode PrintInfo = KeyCode.Space;
			public KeyCode SwitchCamera = KeyCode.Return;
			public KeyCode SwitchAlgorithm = KeyCode.Backspace;
			public KeyCode MoveForward = KeyCode.W;
			public KeyCode MoveBackward = KeyCode.S;
			public KeyCode MoveLeft = KeyCode.A;
			public KeyCode MoveRight = KeyCode.D;
			public KeyCode MoveUp = KeyCode.LeftShift;
			public KeyCode MoveDown = KeyCode.RightShift;
			public KeyCode CameraPitchUp = KeyCode.DownArrow;
			public KeyCode CameraPitchDown = KeyCode.UpArrow;
			public float CameraPitchSpeed = 15.0f;
			public float CameraDistance = 0.25f;
			public float ModelScale = 0.05f;
			public ManualInfo ManualSetting = new ManualInfo();
			public WaypointInfo WaypointSetting = new WaypointInfo();
			public LissajousInfo LissajousSetting = new LissajousInfo();
		}

		static Config config = new Config();

		public void Awake()
		{
			loadConfig();
			DontDestroyOnLoad(this);
		}

		public void LateUpdate()
		{
			if (Input.GetKeyDown(config.Boot))
			{
				var go = GameObject.Find("Maidrone");
				if (go == null)
				{
					loadConfig();
					go = new GameObject("Maidrone");
					go.AddComponent<Drone>();
				}
				else
				{
					Destroy(go);
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
			private enum Algorithm
			{
				Manual,
				Waypoint,
				Lissajous,
			}

			private float m_cameraPitch = 0.0f;
			private Vector3 m_velocity = new Vector3(0.0f, 0.0f, 0.0f);
			private bool m_firstPersonView = false;
			private Algorithm m_alg = Algorithm.Manual;
			private float m_waypointDepartureTime = 0.0f;
			private float m_waypointEstimateTime = 0.0f;
			private int m_prevWaypoint = 0;
			private int m_nextWaypoint = 0;
			private GameObject m_model = null;
			private GameObject m_blade = null;

			void Start()
			{
				var model = new GameObject();
				model.transform.parent = this.gameObject.transform;
				model.transform.localScale = new Vector3(config.ModelScale, config.ModelScale, config.ModelScale);
				var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				body.transform.parent = model.gameObject.transform;
				body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
				var poll = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				poll.transform.parent = model.gameObject.transform;
				poll.transform.localPosition = new Vector3(0f, 0.25f, 0.0f);
				poll.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
				var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
				blade.transform.parent = model.gameObject.transform;
				blade.transform.localPosition = new Vector3(0f, 0.3f, 0.0f);
				blade.transform.localScale = new Vector3(0.5f, 0.01f, 0.1f);

				m_model = model;
				m_blade = blade;
			}

			void Update()
			{
				var config = Maidrone.config;

				if (Input.GetKeyDown(config.PrintInfo))
				{
					Console.WriteLine("Pos=" + transform.position.ToString() + "; Rot=" + new Vector3(m_cameraPitch, transform.eulerAngles.y, 0.0f).ToString());
				}

				if (Input.GetKeyDown(config.SwitchAlgorithm))
				{
					switch (m_alg)
					{
						case Algorithm.Manual:
							m_alg = Algorithm.Waypoint;
							if (config.WaypointSetting.Waypoints.Count == 0)
							{
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
						case Algorithm.Waypoint:
							m_alg = Algorithm.Lissajous;
							break;
						case Algorithm.Lissajous:
							m_alg = Algorithm.Manual;
							break;
					}
				}

				if (m_alg == Algorithm.Manual)
				{
					var man = config.ManualSetting;

					float accXZ = man.AccelXZ * Time.deltaTime;
					float accY = man.AccelY * Time.deltaTime;

					// control player
					if (Input.GetKey(man.RotateRight)) transform.rotation = Quaternion.Euler(0.0f, man.RotSpeed * Time.deltaTime, 0.0f) * transform.rotation;
					if (Input.GetKey(man.RotateLeft)) transform.rotation = Quaternion.Euler(0.0f, -man.RotSpeed * Time.deltaTime, 0.0f) * transform.rotation;
					if (Input.GetKey(config.MoveUp)) m_velocity += transform.up * accY;
					if (Input.GetKey(config.MoveDown)) m_velocity -= transform.up * accY;
					if (Input.GetKey(config.MoveForward)) m_velocity += transform.forward * accXZ;
					if (Input.GetKey(config.MoveBackward)) m_velocity -= transform.forward * accXZ;
					if (Input.GetKey(config.MoveRight)) m_velocity += transform.right * accXZ;
					if (Input.GetKey(config.MoveLeft)) m_velocity -= transform.right * accXZ;
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
					var lsg = config.LissajousSetting;

					if (Input.GetKey(config.MoveUp)) lsg.Position += transform.up * Time.deltaTime;
					if (Input.GetKey(config.MoveDown)) lsg.Position -= transform.up * Time.deltaTime;
					if (Input.GetKey(config.MoveForward)) lsg.Position += transform.forward * Time.deltaTime;
					if (Input.GetKey(config.MoveBackward)) lsg.Position -= transform.forward * Time.deltaTime;
					if (Input.GetKey(config.MoveRight)) lsg.Position += transform.right * Time.deltaTime;
					if (Input.GetKey(config.MoveLeft)) lsg.Position -= transform.right * Time.deltaTime;

					transform.position = new Vector3(
						lsg.Position.x + Mathf.Sin(2.0f * Mathf.PI * lsg.Frequency.x * Time.time + Mathf.Deg2Rad * lsg.Offset.x) * lsg.Amplitude.x,
						lsg.Position.y + Mathf.Sin(2.0f * Mathf.PI * lsg.Frequency.y * Time.time + Mathf.Deg2Rad * lsg.Offset.y) * lsg.Amplitude.y,
						lsg.Position.z + Mathf.Sin(2.0f * Mathf.PI * lsg.Frequency.z * Time.time + Mathf.Deg2Rad * lsg.Offset.z) * lsg.Amplitude.z);
					transform.LookAt( new Vector3(lsg.Position.x, transform.position.y, lsg.Position.z) );

					controlCamera();
				}

				// animation
				if (m_blade != null)
				{
					m_blade.transform.rotation = Quaternion.Euler(0.0f, 1440.0f * Time.deltaTime, 0.0f) * m_blade.transform.rotation;
				}

				// view
				if (Input.GetKeyDown(config.SwitchCamera))
				{
					m_firstPersonView = !m_firstPersonView;
					if (m_model != null)
					{
						for (int i = 0; i < m_model.transform.childCount; i++)
						{
							m_model.transform.GetChild(i).renderer.enabled = !m_firstPersonView;
						}
					}
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
					}
					else
					{
						cam.transform.position = transform.position;
						cam.transform.LookAt(transform.position + Quaternion.AngleAxis(m_cameraPitch, transform.right) * transform.forward);
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
				// control camera
				if (Input.GetKey(config.CameraPitchUp)) m_cameraPitch += config.CameraPitchSpeed * Time.deltaTime;
				if (Input.GetKey(config.CameraPitchDown)) m_cameraPitch -= config.CameraPitchSpeed * Time.deltaTime;
				m_cameraPitch = Mathf.Clamp(m_cameraPitch, -75.0f, 75.0f);
			}
		}
	}
}