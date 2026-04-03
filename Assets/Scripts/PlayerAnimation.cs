using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator anim;
    private PlayerController player;

    void Awake()
    {
        player = GetComponent<PlayerController>();

        // neu quen chua keo Animator tren Inspector thi tu dong tim
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        // bam trigger khi cac event vat ly xay ra
        player.OnJump += TriggerJump;
        player.OnRoll += TriggerRoll;
        player.OnLand += TriggerLand;
    }

    void OnDisable()
    {
        player.OnJump -= TriggerJump;
        player.OnRoll -= TriggerRoll;
        player.OnLand -= TriggerLand;
    }

    void Update()
    {
        anim.SetFloat("speed", Mathf.Abs(player.CurrentInput));
    }

    private void TriggerJump()
    {
        anim.ResetTrigger("doLand");
        anim.ResetTrigger("doRoll"); // Dọn rác
        anim.SetTrigger("doJump");
    }

    private void TriggerRoll()
    {
        anim.ResetTrigger("doJump"); // Dọn rác
        anim.ResetTrigger("doLand"); // Dọn rác
        anim.SetTrigger("doRoll");
    }

    private void TriggerLand()
    {
        anim.ResetTrigger("doJump");
        anim.ResetTrigger("doRoll"); // Dọn rác
        anim.SetTrigger("doLand");
    }
}