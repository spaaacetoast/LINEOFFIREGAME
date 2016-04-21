using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using AngryRain;

public class PlayerInfo : MonoBehaviour 
{
    public Image healthCurrent { get; private set; }
    public Image healthFull { get; private set; }
    public Image healthBackground { get; private set; }

    public Image weaponCurrent { get; private set; }
    public Image weaponFull { get; private set; }
    public Image weaponBackground { get; private set; }

    public Text weaponText { get; private set; }
    public Text weaponCurrentText { get; private set; }
    public Text weaponFullText { get; private set; }

    public void Initialize()
    {
        healthCurrent = transform.Find("Health/current").GetComponent<Image>();
        healthFull = transform.Find("Health/full").GetComponent<Image>();
        healthBackground = transform.Find("Health").GetComponent<Image>();

        weaponCurrent = transform.Find("Weapon/current mag ammo").GetComponent<Image>();
        weaponFull = transform.Find("Weapon/remaining ammo").GetComponent<Image>();
        weaponBackground = transform.Find("Weapon").GetComponent<Image>();

        weaponText = transform.Find("Weapon/Text").GetComponent<Text>();
        weaponCurrentText = transform.Find("Weapon/current mag ammo/Text").GetComponent<Text>();
        weaponFullText = transform.Find("Weapon/remaining ammo/Text").GetComponent<Text>();
    }

    public void UpdateWeaponName(string n)
    {
        weaponText.text = n;
    }

    public void UpdateWeaponAmmoCurrent(int amount)
    {
        weaponCurrentText.text = amount.ToString();
    }

    public void UpdateWeaponAmmoRemaining(int amount)
    {
        weaponFullText.text = amount.ToString();
    }

    public void UpdatePlayerHealth(float health)
    {
        healthCurrent.rectTransform.sizeDelta = new Vector2(220 * (health / 100), 5);
    }
}
