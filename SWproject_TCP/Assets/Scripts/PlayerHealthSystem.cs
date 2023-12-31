using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthSystem : MonoBehaviour
{
	public static PlayerHealthSystem Instance;

	public Image currentHealthBar;
	public Text healthText;
	public float hitPoint = 100f; // ???? ????
	public float maxHitPoint = 100f;

	public Image currentManaBar;
	public Text manaText;
	public float manaPoint = 100f;
	public float maxManaPoint = 100f;

	//==============================================================
	// Regenerate Health & Mana
	//==============================================================
	public bool Regenerate = true;
	public float regen = 0.005f;
	private float timeleft = 0.0f;  // Left time for current interval
	public float regenUpdateInterval = 0.01f;

	public bool GodMode;

	//==============================================================
	// Awake
	//==============================================================
	void Awake()
	{
		Instance = this;
	}

	//==============================================================
	// Awake
	//==============================================================
	void Start()
	{
		UpdateGraphics();
		timeleft = regenUpdateInterval;
		regen = 0.05f;
		regenUpdateInterval = 0.01f;
	}

	//==============================================================
	// Update
	//==============================================================
	void Update()
	{
		if (Regenerate)
			Regen();
	}

	//==============================================================
	// Regenerate Health & Mana
	//==============================================================
	private void Regen()
	{
		timeleft -= Time.deltaTime;

		if (timeleft <= 0.0) // Interval ended - update health & mana and start new interval
		{
			// Debug mode
			if (GodMode)
			{
				HealDamage(maxHitPoint);
				RestoreMana(maxManaPoint);
			}
			else
			{
				HealDamage(regen/40);
				RestoreMana(regen*1.55f*2);
			}

			UpdateGraphics();

			timeleft = regenUpdateInterval;
		}
	}

	//==============================================================
	// Health Logic
	//==============================================================
	private void UpdateHealthBar()
	{
		float ratio = hitPoint / maxHitPoint;
		currentHealthBar.rectTransform.localPosition = new Vector3(currentHealthBar.rectTransform.rect.width * ratio - currentHealthBar.rectTransform.rect.width, 0, 0);
		healthText.text = hitPoint.ToString("0") + "/" + maxHitPoint.ToString("0");
	}

	

	public bool TakeDamage(float Damage)
	{
		hitPoint -= Damage;
		if (hitPoint < 1)
		{
            hitPoint = 0;
            UpdateGraphics();
            return true;
        }
			
		UpdateGraphics();
		return false;

		//StartCoroutine(PlayerHurts());
	}

	public void HealDamage(float Heal)
	{
		hitPoint += Heal;
		if (hitPoint > maxHitPoint)
			hitPoint = maxHitPoint;

		UpdateGraphics();
	}

	public void SetMaxHealth(float max)
	{
		maxHitPoint += (int)(maxHitPoint * max / 100);

		UpdateGraphics();
	}
	//==============================================================
	// Mana Logic
	//==============================================================
	private void UpdateManaBar()
	{
		float ratio = manaPoint / maxManaPoint;
		currentManaBar.rectTransform.localPosition = new Vector3(currentManaBar.rectTransform.rect.width * ratio - currentManaBar.rectTransform.rect.width, 0, 0);
		manaText.text = manaPoint.ToString("0") + "/" + maxManaPoint.ToString("0");
	}


	public bool isEnoughMana(float q)
    {
		if (q > manaPoint)
			return false;
		else
			return true;
    }

	public void UseMana(float Mana)
	{
		manaPoint -= Mana;
		if (manaPoint < 1) // Mana is Zero!!
			manaPoint = 0;

		UpdateGraphics();
	}

	public void RestoreMana(float Mana)
	{
		manaPoint += Mana;
		if (manaPoint > maxManaPoint)
			manaPoint = maxManaPoint;

		UpdateGraphics();
	}
	public void SetMaxMana(float max)
	{
		maxManaPoint += (int)(maxManaPoint * max / 100);

		UpdateGraphics();
	}

	//==============================================================
	// Update all Bars  UI graphics
	//==============================================================
	private void UpdateGraphics()
	{
		UpdateHealthBar();
		UpdateManaBar();
	}

	//==============================================================
	// Coroutine Player Hurts
	//==============================================================
	IEnumerator PlayerHurts()
	{
		// Player gets hurt. Do stuff.. play anim, sound..


		if (hitPoint < 1) // Health is Zero!!
		{
			yield return StartCoroutine(PlayerDied()); // Hero is Dead
		}

		else
			yield return null;
	}

	//==============================================================
	// Hero is dead
	//==============================================================
	IEnumerator PlayerDied()
	{

		yield return null;
	}

}
