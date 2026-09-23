using System;
using UnityEngine;

namespace JumpNotIncluded
{
    public partial class SceneRoot
    {
        public CheckoutStep checkoutStep {get;private set;} = CheckoutStep.Packs;
        public int selectedPack {get;private set;} = 1;
        public PaymentMethod paymentMethod {get;private set;} = PaymentMethod.VirtualCard;
        public string paymentError {get;private set;}
        public string paymentOrderId {get;private set;}
        public float paymentElapsed {get;private set;}
        public const float PaymentDuration=1.4f;
        public TopUpReceipt PaymentReceipt=>Run.Payments.Find(paymentOrderId);
        private bool CheckoutActive=>mode==ScreenMode.Shop&&rechargeOpen;
        private void ResetCheckout()
        {checkoutStep=CheckoutStep.Packs;paymentOrderId=null;paymentError=null;paymentElapsed=0;}
        private void PaymentStep(CheckoutStep step)
        {checkoutStep=step;paymentError=null;toastUntil=0;ui?.ResetFocus();}
        public void SelectPack(int pack)
        {if(CheckoutActive&&checkoutStep==CheckoutStep.Packs&&RunModel.PackCost(pack)>0){selectedPack=pack;paymentError=null;}}
        public void SelectPayment(PaymentMethod method)
        {if(CheckoutActive&&checkoutStep==CheckoutStep.Packs&&(method==PaymentMethod.VirtualCard||method==PaymentMethod.Wallet)){paymentMethod=method;paymentError=null;}}
        public void ContinueCheckout()
        {
            if(!CheckoutActive||checkoutStep!=CheckoutStep.Packs)return;
            PaymentStep(paymentMethod==PaymentMethod.VirtualCard&&!Run.Payments.cardLinked?CheckoutStep.LinkCard:CheckoutStep.Review);
        }
        public void LinkPaymentCard()
        {
            if(!CheckoutActive||checkoutStep!=CheckoutStep.LinkCard)return;
            Run.Payments.LinkCard();SaveCheckpoint(session.checkpointPosition);events.Sound("coin");PaymentStep(CheckoutStep.Review);
        }
        public void UnlinkPaymentCard()
        {
            if(!CheckoutActive||checkoutStep!=CheckoutStep.Packs)return;
            Run.Payments.UnlinkCard();SaveCheckpoint(session.checkpointPosition);ui?.ResetFocus();
        }
        public bool ConfirmTopUp()
        {
            if(!CheckoutActive||checkoutStep!=CheckoutStep.Review)return false;
            paymentError=Run.Payments.Problem(Run,selectedPack,paymentMethod);
            if(paymentError!=null){events.Sound("error");return false;}
            paymentOrderId="JNI-"+Guid.NewGuid().ToString("N").Substring(0,12).ToUpperInvariant();
            paymentElapsed=0;PaymentStep(CheckoutStep.Processing);return true;
        }
        public void AdvancePayment(float seconds)
        {
            if(!CheckoutActive||checkoutStep!=CheckoutStep.Processing||!hasFocus||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            paymentElapsed=Mathf.Min(PaymentDuration,paymentElapsed+seconds);
            if(paymentElapsed<PaymentDuration)return;
            if(!Run.Payments.Purchase(Run,selectedPack,paymentMethod,paymentOrderId,out var error))
            {PaymentStep(CheckoutStep.Review);paymentError=error;events.Sound("error");return;}
            SaveCheckpoint(session.checkpointPosition);events.Sound("coin");PaymentStep(CheckoutStep.Receipt);
        }
        public void ShowPaymentActivity()
        {if(CheckoutActive&&(checkoutStep==CheckoutStep.Packs||checkoutStep==CheckoutStep.Receipt))PaymentStep(CheckoutStep.Activity);}
        public void ShowPaymentReceipt(string id)
        {
            if(!CheckoutActive||checkoutStep!=CheckoutStep.Activity||Run.Payments.Find(id)==null)return;
            paymentOrderId=id;PaymentStep(CheckoutStep.Receipt);
        }
        public void BackToPacks(){if(CheckoutActive){ResetCheckout();ui?.ResetFocus();}}
        public void CloseRecharge()
        {if(CheckoutActive){ResetCheckout();rechargeOpen=false;toastUntil=0;ui?.ResetFocus();}}
    }
}
