
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class KoiColor : UdonSharpBehaviour
{
    public Color AssignPrimaryColor() {
        float roll = Random.Range(0f, 1f);

        if (roll <= 0.01) {
            return PrimaryVeryRare();
        } else if (roll <= 0.15) {
            return PrimaryRare();
        } else if (roll <= 0.50) {
            return PrimaryUncommon();
        } else {
            return PrimaryCommon();
        }
    }

    public Color AssignSecondaryColor() {
        float roll = Random.Range(0f, 1f);

        if (roll <= 0.01) {
            return SecondaryVeryRare();
        } else if (roll <= 0.15) {
            return SecondaryRare();
        } else if (roll <= 0.50) {
            return SecondaryUncommon();
        } else {
            return SecondaryCommon();
        }
    }

    public Color PrimaryVeryRare() {
        Color gold = new Color(1f, 0.73f, 0f);

        Color[] colors = new Color[] { gold };

        int index = Random.Range(0 ,colors.Length-1);
        return colors[index];
    }

    public Color PrimaryRare() {
        Color silver = new Color(0.75f, 0.75f, 0.75f);
        Color black = new Color(0.16f, 0.16f, 0.16f);
        Color white = new Color(0.85f, 0.85f, 0.85f);

        Color[] colors = new Color[] { silver, black, white };

        int index = Random.Range(0, colors.Length-1);
        return colors[index];
    }

    public Color PrimaryUncommon() {
        Color seaFormGreen = new Color(0.25f, 0.72f, 0.55f);
        Color purple = new Color(0.66f, 0.28f, 0.68f);
        Color cardinal = new Color(0.75f, 0.14f, 0.24f);

        Color[] colors = new Color[] { seaFormGreen, purple, cardinal };

        int index = Random.Range(0, colors.Length-1);
        return colors[index];
    }

    public Color PrimaryCommon() {
        Color red = new Color(0.78f, 0.17f, 0.17f);
        Color green = new Color(0.16f, 0.72f, 0.29f);
        Color blue = new Color(0.17f, 0.38f, 0.90f);

        Color[] colors = new Color[] { red, green, blue };

        int index = Random.Range(0, colors.Length-1);
        return colors[index];
    }

    public Color SecondaryVeryRare() {
        Color gold = new Color(1f, 0.73f, 0f);

        Color[] colors = new Color[] { gold };

        int index = Random.Range(0, colors.Length-1);
        return colors[index];
    }

    public Color SecondaryRare() {
        Color silver = new Color(0.75f, 0.75f, 0.75f);
        Color black = new Color(0.16f, 0.16f, 0.16f);
        Color white = new Color(0.85f, 0.85f, 0.85f);

        Color[] colors = new Color[] { silver, black, white };

        int index = Random.Range(0, colors.Length-1);
        return colors[index];
    }

    public Color SecondaryUncommon() {
        Color darkSpringGreen = new Color(0.11f, 0.43f, 0.31f);
        Color grape = new Color(0.31f, 0.07f, 0.32f);
        Color maroonOak = new Color(0.32f, 0.05f, 0.09f);

        Color[] colors = new Color[] { darkSpringGreen, grape, maroonOak };

        int index = Random.Range(0, colors.Length-1);
        return colors[index];
    }

    public Color SecondaryCommon() {
        Color darkRed = new Color(0.48f, 0.15f, 0.15f);
        Color darkGreen = new Color(0.07f, 0.42f, 0.15f);
        Color darkBlue = new Color(0.07f, 0.18f, 0.43f);

        Color[] colors = new Color[] { darkRed, darkGreen, darkBlue };

        int index = Random.Range(0, colors.Length-1);
        return colors[index];
    }
}
