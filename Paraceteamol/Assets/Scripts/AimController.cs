﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AimController : MonoBehaviour
{
	[Space]
	[SerializeField] private ParticleSystem InhaleParticles;
	[SerializeField] private ParticleSystem ExhaleParticles;
	[SerializeField] private GameObject Sight;
	[SerializeField] private GameObject Crosshair;
    [SerializeField] private GameObject chronometer;
	[Space]
	public float Strenght = .5f;
	[Tooltip("Time in seconds the player will not be able to inhale")]
	public float CooldownTime = 0;
	[Tooltip("Time in seconds the player")]
	public float InhaleTime = 2;

	[Header("Analogico esquerdo")]
	public string KeyboardHorizontal = "p1_horizontal";
	public string KeyboardVertical = "p1_vertical";

	[Header("Analogico direito")]
	public string JoystickHorizontal = "p1_ps4_R_horizontal";
	public string JoystickVertical = "p1_ps4_R_vertical";

	[Header("Buttons")]
	public string InhaleButton = "p1_fire1";

	private Vector2 _rightStickInput;
	private float _horizontal;
	private bool _keyboard;
	private GameObject _ballGO;
    public float _timer;

	#region State
	public enum State
	{
		Idle,
		Cooldown,
		Inhale,
		Exhale,
	}
	 
	public State state;

	IEnumerator IdleState()
	{
		while (state == State.Idle)
		{
			yield return 0;
		}
		NextState();
	}

	IEnumerator CooldownState()
	{
		while (state == State.Cooldown)
		{
			yield return 0;
		}
		NextState();
	}

	IEnumerator InhaleState()
	{
		while (state == State.Inhale)
		{
			yield return 0;
		}
		NextState();
	}

	IEnumerator ExhaleState()
	{
		while (state == State.Exhale)
		{
			yield return 0;
		}
		NextState();
	}

	void NextState()
	{
		string methodName = state.ToString() + "State";
		System.Reflection.MethodInfo info =
			GetType().GetMethod(methodName,
								System.Reflection.BindingFlags.NonPublic |
								System.Reflection.BindingFlags.Instance);
		StartCoroutine((IEnumerator)info.Invoke(this, null));
	}
	#endregion
	/* 
     * States:
     *  Idle
	 *  Cooldown
	 *  Inhale
	 *  Exhale
    */

	// Time that the player can keep Inhaling
	private IEnumerator InhaleTimer(GameObject ballGameObject)
	{
		yield return new WaitForSeconds(InhaleTime);

		InhaleParticles.Stop();

		state = State.Exhale;
	}

	// Time the player can't use the inhale
	private IEnumerator Cooldown()
	{
        chronometer.SetActive(true);
        yield return new WaitForSeconds(CooldownTime);
        chronometer.SetActive(false);
        state = State.Idle;
	}


	private void Start()
	{
		_keyboard = GetComponent<PlayerMovement>().Keyboard;
		state = State.Idle;
        _timer = InhaleTime;
        Time.timeScale = 1f;
	}


	private void FixedUpdate()
	{
         
        if (state == State.Inhale)
        {
            _timer -= Time.deltaTime;
            Debug.Log(_timer);
            if (_timer <= 0.0f)
            {
                InhaleParticles.Stop();
                
                state = State.Exhale;
                
            }

           
        }
        else
        _timer = InhaleTime;

		// Testa se vai usar teclado ou controle
		#region Verifica Teclado
		if (_keyboard == true)
		{
			//Inputs horizontais
			if (Input.GetAxis(KeyboardHorizontal) > 0)
				_horizontal = 0;
			else if (Input.GetAxis(KeyboardHorizontal) < 0)
				_horizontal = 180;

			//Inputs Verticais
			if (Input.GetAxis(KeyboardVertical) > 0)
				Sight.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 90));
			else if (Input.GetAxis(KeyboardVertical) < 0)
				Sight.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -90));
			else
				Sight.transform.rotation = Quaternion.Euler(new Vector3(0, _horizontal, 0));
		}
		else
		{
			_rightStickInput = new Vector2(Input.GetAxis(JoystickHorizontal), Input.GetAxis(JoystickVertical));

			if (_rightStickInput.magnitude > 0.1f)
			{
				Vector3 curRotation = Vector3.left * _rightStickInput.x + Vector3.up * _rightStickInput.y;
				Quaternion aimRotation = Quaternion.FromToRotation(new Vector3(-16f, 0, 0), curRotation);
				Sight.transform.rotation = aimRotation;
			}
		}
		#endregion

		switch (state)
		{
			case State.Idle:
				////Debug.Log("Idle");
				if (Input.GetButtonDown(InhaleButton))
					InhaleParticles.Play();
                if (Input.GetButtonUp(InhaleButton))
                {
                    //Debug.Log("Parou de apertar o botão.");
                    InhaleParticles.Stop();
                }
                   
				break;
			case State.Inhale:
                bool hasntSetNewPlayer = true;
				////Debug.Log("Inhale");
				if (Vector2.Distance(_ballGO.transform.position, Crosshair.transform.position) > .1f)
				{
					_ballGO.transform.position = Vector3.MoveTowards(_ballGO.transform.position, Crosshair.transform.position, Strenght);
                    if (hasntSetNewPlayer)
                    {
                        _ballGO.GetComponent<BallPhysics>().SetPlayerCurrentlyHolding(gameObject);
                        hasntSetNewPlayer = false;
                    }
				}
				else
				{
					_ballGO.GetComponent<BallPhysics>().state = BallPhysics.State.Held;
					_ballGO.transform.position = Crosshair.transform.position;
				}
				break;
			case State.Cooldown:
				//Debug.Log("Cooldown");
                StartCoroutine(Cooldown());
                break;
			case State.Exhale:
				//Debug.Log("Exhale");
				_ballGO.GetComponent<BallPhysics>().state = BallPhysics.State.Release;
                state = State.Cooldown;
                Debug.Log(state);
				break;
		}
	}

	private void OnTriggerStay2D(Collider2D col)
	{
		if (col.gameObject.tag == "Ball")
		{
			_ballGO = col.gameObject;

			switch (state)
			{
				case State.Idle:
					if (Input.GetButton(InhaleButton))
					{
						//Debug.Log("Está puxando.");
						col.GetComponent<BallPhysics>().state = BallPhysics.State.Held;
						 
						state = State.Inhale;
					}
					break;
				case State.Inhale:
					if (Input.GetButtonUp(InhaleButton))
					{
						//Debug.Log("Parou de apertar o botão.");
						InhaleParticles.Stop();

					    state = State.Exhale;
                         
					}
					break;
			}
		}
	}
}