using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsWP.API
{
  public class Settings
  {
    private const string _settingsFilename = "teamswp_settings.dat";
    private string _selectedUserPrincipalName;
    private List<AccountSettingsData> _accounts = new List<AccountSettingsData>();

    public Settings()
    {
    }

    public string SelectedUserPrincipalName { get => _selectedUserPrincipalName; set => _selectedUserPrincipalName = value; }
    public List<AccountSettingsData> AccountSettings { get => _accounts; set => _accounts = value; }

    public AccountSettingsData CurrentAccountSettings { get { return _accounts.FirstOrDefault(s => s.Credentials.UserPrincipalName == _selectedUserPrincipalName); } }

    public async Task<bool> ReadSettings()
    {
      try
      {
        var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        var provider = new Windows.Security.Cryptography.DataProtection.DataProtectionProvider();

        var file = await localFolder.GetFileAsync(_settingsFilename);
        var buffProtected = await Windows.Storage.FileIO.ReadBufferAsync(file);

        var buffUnprotected = await provider.UnprotectAsync(buffProtected);
        var strClearText = Windows.Security.Cryptography.CryptographicBuffer.ConvertBinaryToString(Windows.Security.Cryptography.BinaryStringEncoding.Utf8, buffUnprotected);

        Newtonsoft.Json.JsonConvert.PopulateObject(strClearText,this);

        return _accounts.Count > 0;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> WriteSettings()
    {
      try
      {
        var str = Newtonsoft.Json.JsonConvert.SerializeObject(this);

        var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        var provider = new Windows.Security.Cryptography.DataProtection.DataProtectionProvider("LOCAL=user");

        var buffMsg = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(str, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
        var buffProtected = await provider.ProtectAsync(buffMsg);

        var file = await localFolder.CreateFileAsync(_settingsFilename, Windows.Storage.CreationCollisionOption.ReplaceExisting);
        await Windows.Storage.FileIO.WriteBufferAsync(file, buffProtected);

        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> DeleteCurrentAccountSettings()
    {
      try
      {
        var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

        _accounts.Remove(CurrentAccountSettings);
        SelectedUserPrincipalName = _accounts.Count > 0 ? _accounts[0].Credentials.UserPrincipalName : null;
        if (!await WriteSettings())
        {
          return false;
        }
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> DeleteAccountSettings(string UserPrincipalName)
    {
      try
      {
        var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

        _accounts.RemoveAll(s=>s.Credentials.UserPrincipalName == UserPrincipalName);
        if (SelectedUserPrincipalName == UserPrincipalName)
        {
          SelectedUserPrincipalName = _accounts.Count > 0 ? _accounts[0].Credentials.UserPrincipalName : null;
        }
        if (!await WriteSettings())
        {
          return false;
        }
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public struct CredentialsData
    {
      public string UserPrincipalName { get; set; }
      public string AccessToken { get; set; }
      public string RefreshToken { get; set; }
    }

    public class AccountSettingsData
    {
      public CredentialsData Credentials;
    }
  }
}
