using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;


public class ShootingScript : MonoBehaviourPunCallbacks
{
    public Camera camera;

    public GameObject hitEffectPrefab;

    [Header("HP Related Stuff")]
    public float startHealth = 100;
    private float health;
    public Image healthBar;

    private Animator animator;
    public int kills = 0;


    GameManager gameManager;

    void Wake()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }
    // Start is called before the first frame update
    void Start()
    {
        health = startHealth;
        healthBar.fillAmount = health / startHealth;
        animator = this.GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Fire()
    {
        RaycastHit hit;
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (Physics.Raycast(ray, out hit, 200))
        {
            Debug.Log(hit.collider.gameObject.name);

            photonView.RPC("CreateHitEffects", RpcTarget.All, hit.point);

            // AllBuffered means current and future players in room will get this broadcast function
            if (hit.collider.gameObject.CompareTag("Player") && !hit.collider.gameObject.GetComponent<PhotonView>().IsMine)
            {
                hit.collider.gameObject.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.AllBuffered, 25);

                if (hit.collider.gameObject.GetComponent<ShootingScript>().health <= 0)
                {
                    this.gameObject.GetComponent<PhotonView>().RPC("KillCounter", RpcTarget.AllBuffered);
                }
            }
        }
    }

    [PunRPC]
    public void TakeDamage(int damage, PhotonMessageInfo info)
    {
        this.health -= damage;
        this.healthBar.fillAmount = health / startHealth;

        if (health <= 0)
        {
            Die();
            StartCoroutine(KillFeedCounter((string)info.Sender.NickName, (string)info.photonView.Owner.NickName));
            Debug.Log(info.Sender.NickName + " killed " + info.photonView.Owner.NickName);
        }
    }

    [PunRPC]
    public void CreateHitEffects(Vector3 position)
    {
        GameObject hitEffectGameObject = Instantiate(hitEffectPrefab, position, Quaternion.identity);
        Destroy(hitEffectGameObject, 0.2f);
    }


    public void Die()
    {
        if (photonView.IsMine)
        {
            animator.SetBool("isDead", true);
            StartCoroutine(RespawnCounter());

        }
    }

    IEnumerator RespawnCounter()
    {
        GameObject respawnText = GameObject.Find("Respawn Text");
        float respawnTime = 5.0f;

        while (respawnTime > 0)
        {
            yield return new WaitForSeconds(1.0f);
            respawnTime--;


            transform.GetComponent<PlayerMovementController>().enabled = false;
            respawnText.GetComponent<Text>().text = "You are killed. Respawning in " + respawnTime.ToString(".00");
        }
        animator.SetBool("isDead", false);
        respawnText.GetComponent<Text>().text = "";

        int randomPointX = Random.Range(-20, 20);
        int randomPointZ = Random.Range(-20, 20);

        this.transform.position = new Vector3(randomPointX, 0, randomPointZ);
        //this.transform.position = GameManager.instance.setSpawnPoint();
        transform.GetComponent<PlayerMovementController>().enabled = true;

        photonView.RPC("RegainHealth", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void RegainHealth()
    {
        health = 100;
        healthBar.fillAmount = health / startHealth;
    }

    /*public void KillFeed()
    {
            StartCoroutine(KillFeedCounter((string)info.Sender.NickName, (string)info.photonView.Owner.NickName));
    }*/
    public IEnumerator KillFeedCounter (string killer, string dead)
    {
        GameObject killFeedText = GameObject.Find("Kill Feed Text");
        killFeedText.GetComponent<Text>().text = killer + " killed " + dead;

        float killfeedTime = 3.0f;

        while (killfeedTime > 0)
        {
            yield return new WaitForSeconds(1.0f);
            killfeedTime--;
        }
        killFeedText.GetComponent<Text>().text = "";
    }

    [PunRPC]
    public void KillCounter()
    {
        kills++;

        //Exit room and go back to lobby
        if (kills >= 2)
        {
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("LobbyScene");
        }

    }
}
