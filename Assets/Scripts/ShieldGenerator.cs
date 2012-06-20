﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShieldGenerator : Structure
{
    public float CurrentHue;
    float TotalPower;

    public float Health;
    public float CurrentHealth;

    GameObject lightGo, sphere, fountain, shieldInAir;
    public List<EnemyBullet> DefendingAgainst = new List<EnemyBullet>();

    public float EnemyHealth = 500;
    public float? EnemyHue;

    public static ShieldGenerator Instance;

    public bool IsPowered
    {
        get { return Hues.Count > 0; }
    }

    protected override void Start()
    {
        base.Start();

        name = "Shield Generator";

        Instance = this;

        CurrentHealth = Health = 500;
        EnemyHue = 25;
        EnemyHealth = 300;

        lightGo = gameObject.FindChild("Point light");
        sphere = gameObject.FindChild("shieldgenerator").FindChild("Sphere");
        fountain = gameObject.FindChild("shieldgenerator").FindChild("Shield");
        shieldInAir = gameObject.FindChild("Shield");

        lightGo.light.intensity = 0;
        fountain.renderer.enabled = sphere.renderer.enabled = false;
        foreach (var r in shieldInAir.GetComponentsInChildren<Renderer>())
            r.renderer.enabled = false;
    }

    public override void LinkHue(float hue)
    {
        base.LinkHue(hue);

        if (Hues.Count == 1)
        {
            lightGo.light.intensity = 1;
            fountain.renderer.enabled = sphere.renderer.enabled = true;
            foreach (var r in shieldInAir.GetComponentsInChildren<Renderer>())
                r.renderer.enabled = true;

            CurrentHue = Hue;
        }
    }

    public override void UnlinkHue(float hue)
    {
        base.UnlinkHue(hue);

        if (Hues.Count == 0)
        {
            fountain.renderer.enabled = sphere.renderer.enabled = false;
            foreach (var r in shieldInAir.GetComponentsInChildren<Renderer>())
                r.renderer.enabled = false;
            lightGo.light.intensity = 0;
        }
    }

    void Update()
    {
        CurrentHue = Mathf.LerpAngle(CurrentHue, Hue, 0.1f);
        if (CurrentHue < 0) CurrentHue += 360;
        if (CurrentHue > 360) CurrentHue -= 360;

        CurrentHealth = Mathf.Lerp(CurrentHealth, Health, 0.1f);

        foreach (var r in shieldInAir.GetComponentsInChildren<Renderer>())
            r.material.SetColor("_TintColor", ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f));

        sphere.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f));
        fountain.renderer.material.SetColor("_TintColor", ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f));
        lightGo.light.color = ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f);

        // Defend against bullets
        if (!IsPowered) return;
        for (int i = DefendingAgainst.Count - 1; i >= 0; i--)
        {
            var da = DefendingAgainst[i];

            if (!da.IsInitialized) continue;
            if (da.IsAutoDestructed)
            {
                DefendingAgainst.RemoveAt(i);
                continue;
            }

            if (da.transform.position.magnitude < 45)
            {
                var shieldV = new Vector2(Mathf.Cos(Mathf.Deg2Rad * Hue), Mathf.Sin(Mathf.Deg2Rad * Hue)).normalized;
                var bulletV = new Vector2(Mathf.Cos(Mathf.Deg2Rad * da.Hue), Mathf.Sin(Mathf.Deg2Rad * da.Hue)).normalized;

                var malus = (Vector3.Dot(bulletV, shieldV) + 1) / 2;
                if (malus < 0.25f) malus = 0;

                da.Power -= malus * Time.deltaTime * 0.75f;
                da.Power = Mathf.Max(0, da.Power);

                //Debug.Log("dp = " + Vector3.Dot(bulletV, shieldV));
                //Debug.Log("actual malus = " + Easing.EaseIn((Vector3.Dot(bulletV, shieldV) + 1) / 2, EasingType.Quadratic));

                if (da.Power <= 0 && da.CurrentScale <= 0.1f)
                {
                    DefendingAgainst.RemoveAt(i);
                    Destroy(da.gameObject);
                }
            }
        }
    }

    public void FinishGame()
    {
        Networking.RpcEndGame();
        transform.parent.BroadcastMessage("OnDie", SendMessageOptions.DontRequireReceiver);
    }

    public void UpdateEnemyShield(Vector2 data)
    {
        EnemyHue = data.x;
        EnemyHealth = data.y;
    }

    public float? AssaultHue
    {
        get
        {
            if (DefendingAgainst.Count == 0) return null;

            for (int i = 0; i < DefendingAgainst.Count; i++)
                if (!DefendingAgainst[i].IsAbsorbed)
                    return DefendingAgainst[i].Hue;

            return DefendingAgainst[DefendingAgainst.Count - 1].Hue;
        }
    }
}