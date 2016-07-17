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
	[PluginFilter("CM3D2x64"), PluginFilter("CM3D2x86"), PluginName("Maidrone"), PluginVersion("0.0.0.0")]
	public class Maidrone : PluginBase
	{
		public class Config
		{
			public KeyCode Boot = KeyCode.F11;
			public KeyCode MoveForward = KeyCode.W;
			public KeyCode MoveBackward = KeyCode.S;
			public KeyCode MoveLeft = KeyCode.A;
			public KeyCode MoveRight = KeyCode.D;
			public KeyCode MoveUp = KeyCode.LeftShift;
			public KeyCode MoveDown = KeyCode.RightShift;
			public KeyCode RotateLeft = KeyCode.LeftArrow;
			public KeyCode RotateRight = KeyCode.RightArrow;
			public KeyCode RotateUp = KeyCode.DownArrow;
			public KeyCode RotateDown = KeyCode.UpArrow;
			public KeyCode SwitchCamera = KeyCode.Return;
			public float AccelXZ = 0.2f;
			public float AccelY = 0.2f;
			public float RotSpeedYaw = 15.0f;
			public float RotSpeedPitch = 15.0f;
			public float ModelScale = 0.05f;
			public float CameraDistance = 0.25f;
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
			private float m_cameraPitch = 0.0f;
			private Vector3 m_velocity = new Vector3(0.0f, 0.0f, 0.0f);
			private bool m_firstPersonView = false;
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
				float accXZ = config.AccelXZ * Time.deltaTime;
				float accY = config.AccelY * Time.deltaTime;
				float rotYaw = config.RotSpeedYaw;
				float rotPitch = config.RotSpeedPitch;
				bool fpv = m_firstPersonView;

				// control player
				float rotVelocity = 0.0f;
				if (Input.GetKey(config.RotateRight)) rotVelocity = rotYaw;
				if (Input.GetKey(config.RotateLeft)) rotVelocity = -rotYaw;
				transform.rotation = Quaternion.Euler(0.0f, rotVelocity * Time.deltaTime, 0.0f) * transform.rotation;

				if (Input.GetKey(config.MoveUp)) m_velocity += transform.up * accY;
				if (Input.GetKey(config.MoveDown)) m_velocity -= transform.up * accY;
				if (Input.GetKey(config.MoveForward)) m_velocity += transform.forward * accXZ;
				if (Input.GetKey(config.MoveBackward)) m_velocity -= transform.forward * accXZ;
				if (Input.GetKey(config.MoveRight)) m_velocity += transform.right * accXZ;
				if (Input.GetKey(config.MoveLeft)) m_velocity -= transform.right * accXZ;
				transform.position += m_velocity * Time.deltaTime;
				m_velocity -= m_velocity * Mathf.Clamp(Time.deltaTime, 0.0f, 1.0f);

				// control camera
				float pitchVelocity = 0.0f;					
				if (Input.GetKeyDown(config.SwitchCamera)) m_firstPersonView = !m_firstPersonView;
				if (Input.GetKey(config.RotateUp)) pitchVelocity = rotPitch;
				if (Input.GetKey(config.RotateDown)) pitchVelocity = -rotPitch;
				m_cameraPitch += pitchVelocity * Time.deltaTime;
				m_cameraPitch = Mathf.Clamp(m_cameraPitch, -75.0f, 75.0f);

				// animation
				if (m_blade != null)
				{
					m_blade.transform.rotation = Quaternion.Euler(0.0f, 1440.0f * Time.deltaTime, 0.0f) * m_blade.transform.rotation;
				}

				if (m_model != null && fpv != m_firstPersonView)
				{
					for (int i = 0; i < m_model.transform.childCount; i++)
					{
						m_model.transform.GetChild(i).renderer.enabled = !m_firstPersonView;
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
		}
	}
}