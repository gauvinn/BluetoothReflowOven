/*
* Copyright(C) 2009 The Android Open Source Project
*
* Licensed under the Apache License, Version 2.0(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Lang;
using Java.Util;

namespace BluetoothChat
{
	[Activity(Label = "@string/app_name", MainLauncher = true, ConfigurationChanges=Android.Content.PM.ConfigChanges.KeyboardHidden | Android.Content.PM.ConfigChanges.Orientation)]
	public class BluetoothChat : Activity
	{
        #region Debug Variables
        private const string TAG = "BluetoothChat";
		private const bool Debug = true;
        #endregion

        #region ENUMS
        public enum MESSAGE_COMMANDS { MESSAGE_STATE_CHANGE, MESSAGE_READ, MESSAGE_WRITE, MESSAGE_DEVICE_NAME, MESSAGE_TOAST };
        private enum IRC { REQUEST_CONNECT_DEVICE, REQUEST_ENABLE_BT};
        #endregion

        #region Shared Strings
        public const string DEVICE_NAME = "device_name";
        public const string TOAST = "toast";
        #endregion

        #region Bluetooth Variables
        protected string connectedDeviceName = null;
		protected ArrayAdapter<string> conversationArrayAdapter;
		private StringBuffer outStringBuffer;
		private BluetoothAdapter bluetoothAdapter = null;
		private BluetoothChatService chatService = null;
        private Receiver receiver = null;
        #endregion

        #region UI Elements
        protected TextView title;
		private ListView conversationView;
		private EditText outEditText;
		private Button sendButton;
        #endregion

        #region Overriden Activity Methods
        protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			
			if(Debug)
				Log.Error(TAG, "+++ ON CREATE +++");
			
			RequestWindowFeature(WindowFeatures.CustomTitle);
			SetContentView(Resource.Layout.main);
			Window.SetFeatureInt(WindowFeatures.CustomTitle, Resource.Layout.custom_title);
	
			title = FindViewById<TextView>(Resource.Id.title_left_text);
			title.SetText(Resource.String.app_name);
			title = FindViewById<TextView>(Resource.Id.title_right_text);

            bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
	
			if(bluetoothAdapter == null)
            {
				Toast.MakeText(this, "Bluetooth is not available", ToastLength.Long).Show();
				Finish();
				return;
			}
		}
		
		protected override void OnStart()
		{
			base.OnStart();
			
			if(Debug)
				Log.Error(TAG, "++ ON START ++");
			
			if(!bluetoothAdapter.IsEnabled)
            {
				//Intent enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
				//StartActivityForResult(enableIntent, (int)IRC.REQUEST_ENABLE_BT);
			}
            else
            {
				if(chatService == null)
					SetupChat();
                if(receiver == null)
                {
                    receiver = new Receiver(this, bluetoothAdapter, chatService);
                    var filter = new IntentFilter(BluetoothDevice.ActionFound);
                    RegisterReceiver(receiver, filter);
                    
                    filter = new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished);
                    RegisterReceiver(receiver, filter);
                }
                if(chatService.GetState() != BluetoothChatService.STATE_CONNECTED)
                    bluetoothAdapter.StartDiscovery();
            }
		}
		
		protected override void OnResume()
		{
			base.OnResume();
			
			if(chatService != null)
            {
				if(chatService.GetState() == BluetoothChatService.STATE_NONE)
                {
					chatService.Start();
				}
			}
		}
		
		protected override void OnPause()
		{
			base.OnPause();
			
			if(Debug)
				Log.Error(TAG, "- ON PAUSE -");
		}
		
		protected override void OnStop()
		{
			base.OnStop();
			
			if(Debug)
				Log.Error(TAG, "-- ON STOP --");
		}
		
		protected override void OnDestroy()
		{
			base.OnDestroy();
			
			if(chatService != null)
				chatService.Stop();
			
			if(Debug)
				Log.Error(TAG, "--- ON DESTROY ---");
		}
		
		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			if(Debug)
				Log.Debug(TAG, "onActivityResult " + resultCode);
			
			switch((IRC)requestCode)
			{
				case IRC.REQUEST_CONNECT_DEVICE:
					if( resultCode == Result.Ok)
					{
						var address = data.Extras.GetString(DeviceListActivity.EXTRA_DEVICE_ADDRESS);
						BluetoothDevice device = bluetoothAdapter.GetRemoteDevice(address);
						chatService.Connect(device);
					}
					break;
				case IRC.REQUEST_ENABLE_BT:
					if(resultCode == Result.Ok)
					{
						SetupChat();	
					}
					else
					{
						Log.Debug(TAG, "BT not enabled");
						Toast.MakeText(this, Resource.String.bt_not_enabled_leaving, ToastLength.Short).Show();
						Finish();
					}
					break;
			}
		}
		
		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			var inflater = MenuInflater;
			inflater.Inflate(Resource.Menu.option_menu, menu);
			return true;
		}
		
		public override bool OnOptionsItemSelected(IMenuItem item)
		{
            return true;
			switch(item.ItemId) 
			{
				case Resource.Id.scan:
					//var serverIntent = new Intent(this, typeof(DeviceListActivity));
					//StartActivityForResult(serverIntent, (int)IRC.REQUEST_CONNECT_DEVICE);
					return true;
			}
			return false;
		}
        #endregion
		
		private void SendMessage(Java.Lang.String message)
		{
			if(chatService.GetState() != BluetoothChatService.STATE_CONNECTED)
            {
				Toast.MakeText(this, Resource.String.not_connected, ToastLength.Short).Show();
				return;
			}
	        else if(message.Length() > 0)
            {
                // Send message
				byte[] msg = message.GetBytes();
				chatService.Write(msg);
                // Send EOT character
                byte[] END_MSG = new Java.Lang.String("~").GetBytes();
                chatService.Write(END_MSG);
	
				outStringBuffer.SetLength(0);
				outEditText.Text = string.Empty;
			}
		}

        // TODO: Eliminate
        private void SetupChat()
		{
			Log.Debug(TAG, "SetupChat()");
	
			conversationArrayAdapter = new ArrayAdapter<string>(this, Resource.Layout.message);
			conversationView = FindViewById<ListView>(Resource.Id.@in);
			conversationView.Adapter = conversationArrayAdapter;
	
			outEditText = FindViewById<EditText>(Resource.Id.edit_text_out);
			outEditText.EditorAction += delegate(object sender, TextView.EditorActionEventArgs e) 
            {
				if(e.ActionId == ImeAction.ImeNull && e.Event.Action == KeyEventActions.Up)
                {
                    Profile profile = new Profile()
                    {
                        ProfileName = "Test Profile"
                    };
                    for (int i = 0; i < 6; i++)
                    {
                        profile.AddPointAt(i, 50 * i, 100 + 25 * i);
                    }
                    profile.MovePoint(5, 0);
                    profile.DeletePointAt(4);

                    var message = new Java.Lang.String(profile.ConvertToJsonString());
                    SendMessage(message);
                }	
			};
			
			sendButton = FindViewById<Button>(Resource.Id.button_send);
			sendButton.Click += delegate(object sender, EventArgs e) 
            {
                Profile profile = new Profile()
                {
                    ProfileName = "Test Profile"
                };
                for (int i = 0; i < 6; i++)
                {
                    profile.AddPointAt(i, 50 * i, 100 + 25 * i);
                }
                profile.MovePoint(5, 0);
                profile.DeletePointAt(4);

                var message = new Java.Lang.String(profile.ConvertToJsonString());
                SendMessage(message);
            };
			
			chatService = new BluetoothChatService(this, new MyHandler(this), UUID.FromString(UUIDs.SDP));
			outStringBuffer = new StringBuffer("");
		}

        #region Classes
        private class Receiver : BroadcastReceiver
        {
            Activity _chat;
            BluetoothAdapter btAdapter;
            BluetoothChatService chatService;

            public Receiver(Activity chat, BluetoothAdapter btAdapter, BluetoothChatService chatService)
            {
                _chat = chat;
                this.btAdapter = btAdapter;
                this.chatService = chatService;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                string action = intent.Action;

                if(action == BluetoothDevice.ActionFound)
                {
                    BluetoothDevice device =(BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);

                    if (device.Name == "Reflow Oven")
                    {
                        btAdapter.CancelDiscovery();
                        Toast.MakeText(_chat, "Connecting to Reflow Oven", ToastLength.Long).Show();
                        chatService.Connect(device);
                    }
                }
                else if(action == BluetoothAdapter.ActionDiscoveryFinished)
                {
                    _chat.SetProgressBarIndeterminateVisibility(false);
                    _chat.SetTitle(Resource.String.select_device);
                    if(chatService.GetState() != BluetoothChatService.STATE_CONNECTING && 
                       chatService.GetState() != BluetoothChatService.STATE_CONNECTED)
                        Toast.MakeText(_chat, "Reflow Oven Not Found", ToastLength.Long).Show();
                }
            }
        }

        private class MyHandler : Handler
		{
			BluetoothChat bluetoothChat;
			
			public MyHandler(BluetoothChat chat)
			{
				bluetoothChat = chat;	
			}
			
			public override void HandleMessage(Message msg)
			{
				switch((MESSAGE_COMMANDS)msg.What)
                {
				    case MESSAGE_COMMANDS.MESSAGE_STATE_CHANGE:
					    if(Debug)
						    Log.Info(TAG, "MESSAGE_STATE_CHANGE: " + msg.Arg1);
					    switch(msg.Arg1)
                        {
					        case BluetoothChatService.STATE_CONNECTED:
					    	    bluetoothChat.title.SetText(Resource.String.title_connected_to);
					    	    bluetoothChat.title.Append(bluetoothChat.connectedDeviceName);
					    	    bluetoothChat.conversationArrayAdapter.Clear();
					    	    break;
					        case BluetoothChatService.STATE_CONNECTING:
					    	    bluetoothChat.title.SetText(Resource.String.title_connecting);
					    	    break;
					        case BluetoothChatService.STATE_LISTEN:
					        case BluetoothChatService.STATE_NONE:
					    	    bluetoothChat.title.SetText(Resource.String.title_not_connected);
					    	    break;
					    }
					    break;
				    case MESSAGE_COMMANDS.MESSAGE_WRITE:
					    byte[] writeBuf =(byte[])msg.Obj;
					    var writeMessage = new Java.Lang.String(writeBuf);
					    bluetoothChat.conversationArrayAdapter.Add("Me: " + writeMessage);
					    break;
				    case MESSAGE_COMMANDS.MESSAGE_READ:
					    byte[] readBuf =(byte[])msg.Obj;
					    var readMessage = new Java.Lang.String(readBuf, 0, msg.Arg1);
					    bluetoothChat.conversationArrayAdapter.Add(bluetoothChat.connectedDeviceName + ":  " + readMessage);
					    break;
				    case MESSAGE_COMMANDS.MESSAGE_DEVICE_NAME:
					    bluetoothChat.connectedDeviceName = msg.Data.GetString(DEVICE_NAME);
					    Toast.MakeText(Application.Context, "Connected to " + bluetoothChat.connectedDeviceName, ToastLength.Short).Show();
					    break;
				    case MESSAGE_COMMANDS.MESSAGE_TOAST:
					    Toast.MakeText(Application.Context, msg.Data.GetString(TOAST), ToastLength.Short).Show();
					    break;
				}
			}
		}
        #endregion
    }
}