﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Capsule : Structure
{
    GameObject capsule;
    float OldHue;
    float CurrentHue;

    static List<Capsule> PropagationList = new List<Capsule>();

    protected override void Start()
    {
        base.Start();

        capsule = gameObject.FindChild("capsule");
        capsule.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(Hue, 1, 0.5f));

        capsule.transform.localPosition = -2500 * Vector3.up;
        Wait.Until(t =>
        {
            var step = Easing.EaseOut(Mathf.Clamp01(t * 4), EasingType.Cubic);
            capsule.transform.localPosition = Vector3.Lerp(-2500 * Vector3.up, Vector3.zero, step);
            return step >= 1;
        }, () =>
        {
            capsule.transform.localPosition = Vector3.zero;
        });
    }

    public override void LinkHue(float hue)
    {
        if (Hues.Count > 0)
            OldHue = Hue;

        base.LinkHue(hue);

        if (Hues.Count == 1)
        {
            if (capsule != null)
                capsule.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(Hue, 1, 0.5f));
            CurrentHue = Hue;
        }

        PropagateHue();
    }

    public override void UnlinkHue(float hue)
    {
        if (Hues.Count > 0)
            OldHue = Hue;

        base.UnlinkHue(hue);

        if (Hues.Count == 0)
        {
            Hues.Add(OldHue);
            capsule.transform.localPosition = -2500 * Vector3.up;
            Wait.Until(t =>
            {
                var step = Easing.EaseOut(Mathf.Clamp01(t * 4), EasingType.Cubic);
                capsule.transform.localPosition = Vector3.Lerp(-2500 * Vector3.up, Vector3.zero, 1 - step);
                return step >= 1;
            }, () =>
            {
                capsule.transform.localPosition = Vector3.zero;

                if (LinkTo != null)
                {
                    var or = LinkTo.OldResource;

                    LinkTo.Unlink();

                    //Structures.Add(or);
                    foreach (var c in or.GetComponentsInChildren<Collider>()) c.enabled = true;
                    foreach (var r in or.GetComponentsInChildren<Renderer>()) r.enabled = true;
                    or.Reset();
                }

                Dispose();
                Destroy(gameObject);
            });
        }
        else
            PropagateHue();
    }

    void Update()
    {
        CurrentHue = Mathf.LerpAngle(CurrentHue, Hue, 0.1f);
        capsule.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f));
    }

    public void PropagateHue()
    {
        if (LinkTo == null) return;

        PropagationList.Add(this);

        LinkTo.Hue = Hue;

        if (LinkTo.To == null) return;
        
        var destinationCapsule = LinkTo.To.GetComponent<Capsule>();
        var destinationCannon = LinkTo.To.GetComponent<Cannon>();
        var destinationShield = LinkTo.To.GetComponent<ShieldGenerator>();

        if (destinationCapsule != null && !PropagationList.Contains(destinationCapsule))
        {
            destinationCapsule.Hues.Remove(OldHue);
            destinationCapsule.Hues.Add(Hue);
            destinationCapsule.PropagateHue();
        }
        else if (destinationCannon != null)
        {
            // TODO
        }
        else if (destinationShield != null)
        {
            // TODO
        }

        PropagationList.Remove(this);
    }
}