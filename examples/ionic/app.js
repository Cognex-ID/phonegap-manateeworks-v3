/*** Manateeworks Plugin Integration IONIC Example ***/

// Ionic Starter App

// angular.module is a global place for creating, registering and retrieving Angular modules
// 'starter' is the name of this angular module example (also set in a <body> attribute in index.html)
// the 2nd parameter is an array of 'requires'
angular.module('starter', ['ionic']).run(function($ionicPlatform) {
   $ionicPlatform.ready(function() {

      //Manateeworks comment
      // Once the cordova is ready call our scannerConfig which should automatically be copied in [wwwfolder]/js/MWBConfig.js
      if(window.cordova){
        scannerConfig();
      }

      if(window.cordova && window.cordova.plugins.Keyboard) {
        // Hide the accessory bar by default (remove this to show the accessory bar above the keyboard
        // for form inputs)
        cordova.plugins.Keyboard.hideKeyboardAccessoryBar(true);
        // Don't remove this line unless you know what you are doing. It stops the viewport
        // from snapping when text inputs are focused. Ionic handles this internally for
        // a much nicer keyboard experience.
        cordova.plugins.Keyboard.disableScroll(true);

      }
      if(window.StatusBar) {
        StatusBar.styleDefault();
      }
    });
})
.controller('mainscanner',function($scope){
    //we added in the index.html file an angular controller that will deal with the button scan and update of the result field
    $scope.startScanner = function(){
      //alert(window.cordova);
      if(window.cordova) {
      
        mwbScanner.setCallback(function(result){
          if(result && result.code){
            //you need to wrap into apply so that view will update the result immediately
            $scope.$apply(function(){
              $scope.barcoderesult = result.code;
              });
          }
          else
            console.log('No Result');
        });
        mwbScanner.startScanning(0,0,50,50);
      
      }
    }
});
