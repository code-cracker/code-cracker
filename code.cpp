#include <iostream>
#include <math.h>

using namespace std;


int main()
{
   long long  int a,b,temp,i,t=0,prod=1,c,d;
   cin>>a>>b;

   if(a>b){
    a=a+b;
    b=a-b;
    a=a-b;
   }
   for(i=2;i<=a/2;i++){
       t=0;

          if(a%i==0){
            if(b%i==0){
                    c=a;
            d=b;
               while(c%i==0&&d%i==0){
                    t++;

                    c=c/i;
                    d=d/i;

                     }

               prod=prod*(t+1);
    }

   }}

   cout<<prod;

}
