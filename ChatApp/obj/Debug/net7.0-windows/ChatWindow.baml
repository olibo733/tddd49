   M S B A M L   `   `   `  ����B  >ChatApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null!clr-namespace:ChatAppChatApp  5   	       ChatApp.ChatWindow   5   	   W SPresentationCore, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   'localclr-namespace:ChatApp     5   	     � Title$  
ChatWindow��5   	   $	��450��6   $
��1000��6)    � 	Resources 5
         BoolToVisConverter��%   &	        �� 5   
   6'   5      .��5      �  � ColumnDefinitions 5   
   �� 5      $	��200 �6   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5"   
   �� $��0��6   $��0��6"   $��5q�6/   ��5$      �� *SearchQuery+ �� UpdateSourceTrigger$ PropertyChanged=��� 5'   
   -   5        $��0��5'      $��1��6"   $��5q�6/   ��5(      �� *SearchedConversationPartners+ �� SelectedItem 5)      �� *SelectedConversationPartner+��5+      �� 5,      .R�5-      �� $
��Bold"�61   ��6    �� 61   5.      50      �� 54   
   $��1��6   $��1��6"   $��10q�6P   ��6/   �� *
Messages+��55      �� 56      .R�57      �� .+�58      �� $
��Bold"�6<   ��6$   �� *Sender+6<   �� 59      $
��Bold"�6:   ��6$   �� *Date+6:   �� 5:      $
��Wrap=�6=   ��6$   �� *	Message+6=   5;      5<      5>      �� 5A   
   $��1��6   $��2��6%    �� Orientation$ 
Horizontal=�62   $��10q�6K   .+�5B      �� $	��600��6   $��5q�6"   ��5C      �� *MessageText+$ PropertyChanged=� �� Mode$ TwoWay=��� 5D      $
��Send��6   $	��100��6$   $��5q�60   ��5E      �� *SendMessageCommand+5F      �� 5I   
   $��1��6   $��3��6%   $ 
Horizontal=�62   $��Center=�6K   $��10q�6h   .+�5J      �� $��Accept��67   $	��100��6H   $��5q�6T   ��6   �� *AcceptCommand+y�5K      �� *IsConnectionRequestPending+ �� 	Converter# [  �� 5L      $
��Deny��65   $	��100��6D   $��5q�6P   ��6   �� *DenyCommand+y�5M      �� *IsConnectionRequestPending+# [  �� 5N      $��
Disconnect��6;   $	��100��6P   $��5q�6\   ��6   �� *DisconnectCommand+6\   �� 5O      $��
Play Sound��6:   $	��100��6O   $��5q�6[   ��6   �� *PlaySoundCommand+6[   5P      5Q      5R      