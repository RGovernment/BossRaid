using Firebase.Auth;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class FirebaseAuthService : MonoBehaviour
{
    public static event Action<FirebaseUser> OnAuthChanged;

   [SerializeField] private TMP_InputField emailInput;
   [SerializeField] private TMP_InputField passwordInput;
   [SerializeField] private TMP_Text messageText;

    private FirebaseAuth Auth => FirebaseBootstrap.Auth;

    private async void Start()
    {
        while(Auth == null)
        {
            await Task.Yield();
        }

        if(Auth.CurrentUser != null)
        {
            OnAuthChanged?.Invoke(Auth.CurrentUser);
        }

    }


    private bool TryGetAuth()
    {
        if(Auth == null)
        {
            Debug.Log("아직 준비 안됨");
            return false;
        }
        return true;
    }

    public async void OnClickSignUp()
    {
        if (!TryGetAuth())
            return;

        try
        {
            var result = await Auth.CreateUserWithEmailAndPasswordAsync(emailInput.text, passwordInput.text);
            OnAuthChanged?.Invoke(result.User);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.GetBaseException().Message);
        }
    }

    public async void OnClickSignIn()
    {
        if (!TryGetAuth())
            return;

        try
        {
            var result = await Auth.SignInWithEmailAndPasswordAsync(emailInput.text, passwordInput.text);
            OnAuthChanged?.Invoke(result.User);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.GetBaseException().Message);
        }
    }

    public async void OnClickAnonymousSignIn()
    {
        if (!TryGetAuth())
            return;

        try
        {
            var result = await Auth.SignInAnonymouslyAsync();
            OnAuthChanged?.Invoke(result.User);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.GetBaseException().Message);
        }
    }

    public void OnClickSignOut()
    {
        if (!TryGetAuth())
            return;

        Auth.SignOut();
        OnAuthChanged?.Invoke(null);
    }

}
