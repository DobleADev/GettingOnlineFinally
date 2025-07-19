using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class LanPlayer : NetworkBehaviour
{
	// --- Referencias y Configuración de Movimiento ---
	[SerializeField] private GameObject _ui;
	[SerializeField] private GameObject _cam;
	[SerializeField] CharacterController _cc;
	[SerializeField] Joystick _joystick;
	[SerializeField] PlatformerCharacterController physics;
	[SerializeField] private Vector3 walkVelocity;
	[SerializeField] private Vector3 pushVelocity;
	[SerializeField] private float moveSpeed = 6;
	[SerializeField] private float jumpHeight = 3;
	[SerializeField] private Vector2 walkInput;
	[SerializeField] private float gravityScale;
	[SerializeField] private float pushFriction;
	[SerializeField] private float groundFriction;
	[SerializeField] private float rotationSpeed = 100f;

	private bool jumpRequested;
	private bool canJump = true;

	[Header("Respawn Settings")]
	[SerializeField] private Vector3 initialSpawnPoint;
	private Vector3 currentSafePoint;

	[Header("Visuals & Animation")]
	[SerializeField] private NetworkTransformChild networkTransformChild;
	public RuntimeAnimatorController animatorController;
	public GameObject[] playerSkinPrefabs;
	[SyncVar(hook = "OnSkinIndexChanged")]
	private int currentSkinIndex = 0;

	// Referencia al GameObject de la skin instanciada en el runtime
	private GameObject currentSkinInstance;
	// Referencia al Animator de la skin activa
	private Animator currentAnimator;

	// --- NUEVO: Referencia al NetworkAnimator ---
	private NetworkAnimator networkAnimator;


	// --- Ciclo de Vida UNET ---
	public override void OnStartLocalPlayer()
	{
		Debug.Log("OnStartLocalPlayer: Jugador local activado.");
		currentSafePoint = initialSpawnPoint;
		_ui.SetActive(true);
		_cam.SetActive(true);
		physics = new PlatformerCharacterController(_cc); // Descomenta si necesitas instanciarlo aquí

		// Obtener el NetworkAnimator del GameObject raíz del jugador
		// Esto es crucial porque NetworkAnimator debe estar en el mismo GameObject que NetworkIdentity.
		networkAnimator = GetComponent<NetworkAnimator>();
		if (networkAnimator == null)
		{
			Debug.LogError("NetworkAnimator no encontrado en el root del LanPlayer. Añadelo al Prefab de LanPlayer.");
		}
	}

	public override void OnStartClient()
	{
		// Obtener el NetworkAnimator también en los clientes para referencia
		networkAnimator = GetComponent<NetworkAnimator>();
		SetPlayerSkin(currentSkinIndex);

	}

	// --- Métodos Principales ---
	void Update()
	{
		if (!isLocalPlayer)
		{
			return;
		}

		walkInput = _joystick.Direction;
		HandleMovement();

		if (physics != null && physics.isOnStableGround)
		{
			canJump = true;
		}

		// --- NUEVO: Sincronizar parámetros del Animator para el salto y el suelo ---
		if (currentAnimator != null && networkAnimator != null)
		{
			// Asume que tienes un parámetro booleano "IsGrounded" en tu Animator
			currentAnimator.SetBool("IsGrounded", physics.isOnStableGround);
			// Si el Animator usa un parámetro "IsJumping" para el estado de salto
			// currentAnimator.SetBool("IsJumping", !physics.isOnStableGround && !canJump);
		}
	}

	// --- Manejo de Movimiento ---
	void HandleMovement()
	{
		if (_cc != null && !_cc.enabled) return;
		if (physics == null) return;

		Vector3 jumpVelocity = Vector3.zero;

		if (jumpRequested && canJump)
		{
			jumpVelocity = jumpHeight * Vector3.up;
			jumpRequested = false;
			canJump = false;
			physics.DetachFromGround();

			// --- NUEVO: Disparar Trigger de salto a través de NetworkAnimator ---
			if (networkAnimator != null)
			{
				networkAnimator.SetTrigger("Jump"); // Asume un Trigger llamado "Jump" en tu Animator Controller
			}
		}

		walkVelocity = Vector3.MoveTowards(walkVelocity, moveSpeed * Vector3.ClampMagnitude(new Vector3(walkInput.x, 0, walkInput.y), 1), groundFriction * Time.deltaTime);
		physics.gravityScale = gravityScale;

		Vector3 previousPosition = transform.position;
		physics.Move((Time.deltaTime * (walkVelocity + pushVelocity)) + jumpVelocity);
		Vector3 currentPosition = transform.position;

		pushVelocity = Vector3.MoveTowards(pushVelocity, Vector3.zero, pushFriction * Time.deltaTime);

		Vector3 moveDirection = currentPosition - previousPosition;
		moveDirection.y = 0;
		moveDirection.Normalize();

		if (currentSkinInstance != null && moveDirection != Vector3.zero)
		{
			Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
			currentSkinInstance.transform.localRotation = Quaternion.RotateTowards(currentSkinInstance.transform.localRotation, toRotation, rotationSpeed * Time.deltaTime);
		}

		if (currentAnimator != null)
		{
			// --- NUEVO: Sincronizar el parámetro "Speed" a través de NetworkAnimator ---
			// NetworkAnimator se encargará de esto si el Animator está en el mismo GO,
			// pero como la skin es un hijo, lo ajustaremos manualmente y NetworkAnimator
			// se enlazará al Animator correcto.
			currentAnimator.SetFloat("Speed", new Vector2(walkInput.x, walkInput.y).magnitude);
		}
	}

	public void Jump()
	{
		if (isLocalPlayer && canJump)
		{
			jumpRequested = true;
			Debug.Log("Jump requested!");
		}
	}

	// --- Comandos (Cliente -> Servidor) ---
	[Command]
	public void CmdSetSafePoint(Vector3 newSafePoint)
	{
		currentSafePoint = newSafePoint;
		Debug.Log("Servidor: Punto seguro actualizado para jugador " + connectionToClient.connectionId + " a " + newSafePoint);
	}

	[Command]
	public void CmdRespawnPlayer()
	{
		Debug.Log("Servidor: Repawneando al jugador " + connectionToClient.connectionId + " en " + currentSafePoint);
		RpcRespawn(currentSafePoint);
	}

	[Command]
	public void CmdChangeSkin(int newSkinIndex)
	{
		if (newSkinIndex >= 0 && newSkinIndex < playerSkinPrefabs.Length)
		{
			currentSkinIndex = newSkinIndex;
			Debug.Log("Servidor: Cambiando skin del jugador " + connectionToClient.connectionId + " a indice " + newSkinIndex);
		}
		else
		{
			Debug.LogWarning("Servidor: Indice de skin invalido recibido: " + newSkinIndex);
		}
	}

	// --- RPCs (Servidor -> Todos los Clientes) ---
	[ClientRpc]
	void RpcRespawn(Vector3 respawnPosition)
	{
		Debug.Log("RPC: Repawneando jugador a " + respawnPosition);
		transform.position = respawnPosition;
	}

	// --- Función de Hook de SyncVar ---
	void OnSkinIndexChanged(int newSkinIndex)
	{
		Debug.Log("Hook: OnSkinIndexChanged llamado con indice " + newSkinIndex);
		SetPlayerSkin(newSkinIndex);
	}

	// --- Gestión de Skins ---
	void SetPlayerSkin(int index)
	{
		if (playerSkinPrefabs == null || playerSkinPrefabs.Length == 0)
		{
			Debug.LogWarning("No hay prefabs de skins asignados al LanPlayer.");
			return;
		}

		if (index < 0 || index >= playerSkinPrefabs.Length)
		{
			Debug.LogWarning("Indice de skin fuera de rango: " + index + ". Usando la skin por defecto (0).");
			index = 0;
		}

		// Destruye la skin actual si existe
		if (currentSkinInstance != null)
		{
			Destroy(currentSkinInstance);
			currentSkinInstance = null;
			currentAnimator = null;
		}

		GameObject selectedSkinPrefab = playerSkinPrefabs[index];
		if (selectedSkinPrefab != null)
		{
			currentSkinInstance = (GameObject)Instantiate(selectedSkinPrefab, transform.position, transform.rotation);
			currentSkinInstance.SetActive(true);
			currentSkinInstance.transform.parent = transform;
			currentSkinInstance.transform.localPosition = Vector3.zero;
			currentSkinInstance.transform.localRotation = Quaternion.identity;

			currentAnimator = currentSkinInstance.GetComponent<Animator>();
			if (currentAnimator == null)
			{
				currentAnimator = currentSkinInstance.GetComponentInChildren<Animator>();
			}

			if (currentAnimator == null)
			{
				Debug.LogWarning("No se encontró Animator en la skin instanciada o en sus hijos para el indice " + index);
			}
			else
			{
				// --- CRUCIAL: Enlaza el NetworkAnimator al Animator de la skin instanciada ---
				// Esto permite que el NetworkAnimator sincronice los parámetros del Animator de la skin.
				if (networkAnimator != null)
				{
					networkAnimator.animator = currentAnimator;
					ApplyAnimatorController();
					networkTransformChild.target = currentAnimator.transform;
					Debug.Log("NetworkAnimator enlazado al Animator de la nueva skin.");
				}
			}
		}
		else
		{
			Debug.LogError("El prefab de skin en el indice " + index + " es nulo.");
		}
	}
	[ContextMenu("Apply AnimatorController")]
	void ApplyAnimatorController()
	{
		currentAnimator.runtimeAnimatorController = animatorController;
		Debug.Log(animatorController.name + " applied to current animation controller: " + currentAnimator.gameObject.name);
	}

	public void Cheer()
	{
		currentAnimator.Play("Cheer");
	}
}