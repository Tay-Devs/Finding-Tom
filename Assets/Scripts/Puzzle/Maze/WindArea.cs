using System.Collections;
using UnityEngine;

public class WindArea : MonoBehaviour
{
    public float windForce = 10f;
    public float windCooldown = 3f;
    public Vector3 windDirection = Vector3.forward;
    public bool windActive = true; // Toggle wind effect

    private void OnTriggerStay(Collider other)
    {
        if (!windActive) return; // Stop wind when disabled

        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(windDirection.normalized * windForce, ForceMode.Acceleration);
            }
        }
    }

    public void SetWindOff()
    {
        windActive = false;
    }
    public void ToggleWindWithCooldown()
    {
        StartCoroutine(WindCooldown());
    }

    private IEnumerator WindCooldown()
    {
        windActive = false;
        yield return new WaitForSeconds(windCooldown);
        windActive = true;
    } 
}