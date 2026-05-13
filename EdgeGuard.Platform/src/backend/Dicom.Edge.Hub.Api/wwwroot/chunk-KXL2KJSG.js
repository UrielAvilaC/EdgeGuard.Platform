import{$ as w,$a as xi,Aa as Sn,Ac as Pi,Ba as gi,C as oi,Ca as ye,Cc as kn,D as Mt,Da as bi,E as si,Ea as Se,Fa as vi,G as ci,Ga as Je,Gc as Bt,H as ui,Ha as yi,Hb as Ri,Ia as Mn,Ib as Oi,Ja as kt,Jb as Re,Jc as z,Ka as ue,Kc as Nn,La as Me,Ma as Di,Mc as Li,N as xt,Na as _i,Nc as W,O as li,Oa as Ei,Ob as ki,Oc as nt,P as di,Pa as wi,Pb as On,Q as Ft,Qa as Ai,Qb as tt,Qc as Bi,Ra as Ci,S as fi,Sa as Ii,Ta as ne,U as F,Ua as Ti,Ub as Oe,Vb as le,X as g,Xa as xe,Y as P,Ya as Si,Za as Nt,_ as C,_a as Mi,a as ni,aa as l,ba as Cn,bb as Fe,c as Tt,ca as mi,cb as qe,cc as Ni,da as Ze,db as xn,dc as ke,e as $,ea as hi,ec as Lt,f as ri,fb as Fi,ja as U,jb as V,ka as I,kb as L,l as Ke,la as In,lb as B,mb as Fn,na as Rt,nb as Qe,oa as O,p as J,pa as Tn,q as ii,rb as Pt,sa as Xe,t as ai,ua as pi,ub as Rn,va as Ot,wa as Te,wb as et,z as St,za as H}from"./chunk-T2F57IQD.js";import{a as R,b as It}from"./chunk-HIUAR4UN.js";var ji=null;function re(){return ji}function Pn(t){ji??=t}var rt=class{},Ne=(()=>{class t{historyGo(e){throw new Error("")}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:()=>l(Ui),providedIn:"platform"})}return t})();var Ui=(()=>{class t extends Ne{_location;_history;_doc=l(I);constructor(){super(),this._location=window.location,this._history=window.history}getBaseHrefFromDOM(){return re().getBaseHref(this._doc)}onPopState(e){let n=re().getGlobalEventTarget(this._doc,"window");return n.addEventListener("popstate",e,!1),()=>n.removeEventListener("popstate",e)}onHashChange(e){let n=re().getGlobalEventTarget(this._doc,"window");return n.addEventListener("hashchange",e,!1),()=>n.removeEventListener("hashchange",e)}get href(){return this._location.href}get protocol(){return this._location.protocol}get hostname(){return this._location.hostname}get port(){return this._location.port}get pathname(){return this._location.pathname}get search(){return this._location.search}get hash(){return this._location.hash}set pathname(e){this._location.pathname=e}pushState(e,n,i){this._history.pushState(e,n,i)}replaceState(e,n,i){this._history.replaceState(e,n,i)}forward(){this._history.forward()}back(){this._history.back()}historyGo(e=0){this._history.go(e)}getState(){return this._history.state}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:()=>new t,providedIn:"platform"})}return t})();function Hi(t,r){return t?r?t.endsWith("/")?r.startsWith("/")?t+r.slice(1):t+r:r.startsWith("/")?t+r:`${t}/${r}`:t:r}function zi(t){let r=t.search(/#|\?|$/);return t[r-1]==="/"?t.slice(0,r-1)+t.slice(r):t}function de(t){return t&&t[0]!=="?"?`?${t}`:t}var jt=(()=>{class t{historyGo(e){throw new Error("")}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:()=>l(Ws),providedIn:"root"})}return t})(),Vs=new C(""),Ws=(()=>{class t extends jt{_platformLocation;_baseHref;_removeListenerFns=[];constructor(e,n){super(),this._platformLocation=e,this._baseHref=n??this._platformLocation.getBaseHrefFromDOM()??l(I).location?.origin??""}ngOnDestroy(){for(;this._removeListenerFns.length;)this._removeListenerFns.pop()()}onPopState(e){this._removeListenerFns.push(this._platformLocation.onPopState(e),this._platformLocation.onHashChange(e))}getBaseHref(){return this._baseHref}prepareExternalUrl(e){return Hi(this._baseHref,e)}path(e=!1){let n=this._platformLocation.pathname+de(this._platformLocation.search),i=this._platformLocation.hash;return i&&e?`${n}${i}`:n}pushState(e,n,i,a){let o=this.prepareExternalUrl(i+de(a));this._platformLocation.pushState(e,n,o)}replaceState(e,n,i,a){let o=this.prepareExternalUrl(i+de(a));this._platformLocation.replaceState(e,n,o)}forward(){this._platformLocation.forward()}back(){this._platformLocation.back()}getState(){return this._platformLocation.getState()}historyGo(e=0){this._platformLocation.historyGo?.(e)}static \u0275fac=function(n){return new(n||t)(w(Ne),w(Vs,8))};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();var Vi=(()=>{class t{_subject=new $;_basePath;_locationStrategy;_urlChangeListeners=[];_urlChangeSubscription=null;constructor(e){this._locationStrategy=e;let n=this._locationStrategy.getBaseHref();this._basePath=Ks(zi($i(n))),this._locationStrategy.onPopState(i=>{this._subject.next({url:this.path(!0),pop:!0,state:i.state,type:i.type})})}ngOnDestroy(){this._urlChangeSubscription?.unsubscribe(),this._urlChangeListeners=[]}path(e=!1){return this.normalize(this._locationStrategy.path(e))}getState(){return this._locationStrategy.getState()}isCurrentPathEqualTo(e,n=""){return this.path()==this.normalize(e+de(n))}normalize(e){return t.stripTrailingSlash(Ys(this._basePath,$i(e)))}prepareExternalUrl(e){return e&&e[0]!=="/"&&(e="/"+e),this._locationStrategy.prepareExternalUrl(e)}go(e,n="",i=null){this._locationStrategy.pushState(i,"",e,n),this._notifyUrlChangeListeners(this.prepareExternalUrl(e+de(n)),i)}replaceState(e,n="",i=null){this._locationStrategy.replaceState(i,"",e,n),this._notifyUrlChangeListeners(this.prepareExternalUrl(e+de(n)),i)}forward(){this._locationStrategy.forward()}back(){this._locationStrategy.back()}historyGo(e=0){this._locationStrategy.historyGo?.(e)}onUrlChange(e){return this._urlChangeListeners.push(e),this._urlChangeSubscription??=this.subscribe(n=>{this._notifyUrlChangeListeners(n.url,n.state)}),()=>{let n=this._urlChangeListeners.indexOf(e);this._urlChangeListeners.splice(n,1),this._urlChangeListeners.length===0&&(this._urlChangeSubscription?.unsubscribe(),this._urlChangeSubscription=null)}}_notifyUrlChangeListeners(e="",n){this._urlChangeListeners.forEach(i=>i(e,n))}subscribe(e,n,i){return this._subject.subscribe({next:e,error:n??void 0,complete:i??void 0})}static normalizeQueryParams=de;static joinWithSlash=Hi;static stripTrailingSlash=zi;static \u0275fac=function(n){return new(n||t)(w(jt))};static \u0275prov=g({token:t,factory:()=>Gs(),providedIn:"root"})}return t})();function Gs(){return new Vi(w(jt))}function Ys(t,r){if(!t||!r.startsWith(t))return r;let e=r.substring(t.length);return e===""||["/",";","?","#"].includes(e[0])?e:r}function $i(t){return t.replace(/\/index.html$/,"")}function Ks(t){if(new RegExp("^(https?:)?//").test(t)){let[,e]=t.split(/\/\/[^\/]+/);return e}return t}var Un=(function(t){return t[t.Decimal=0]="Decimal",t[t.Percent=1]="Percent",t[t.Currency=2]="Currency",t[t.Scientific=3]="Scientific",t})(Un||{});var q={Decimal:0,Group:1,List:2,PercentSign:3,PlusSign:4,MinusSign:5,Exponential:6,SuperscriptingExponent:7,PerMille:8,Infinity:9,NaN:10,TimeSeparator:11,CurrencyDecimal:12,CurrencyGroup:13};function Pe(t,r){let e=On(t),n=e[tt.NumberSymbols][r];if(typeof n>"u"){if(r===q.CurrencyDecimal)return e[tt.NumberSymbols][q.Decimal];if(r===q.CurrencyGroup)return e[tt.NumberSymbols][q.Group]}return n}function Gi(t,r){return On(t)[tt.NumberFormats][r]}var Zs=/^(\d+)?\.((\d+)(-(\d+))?)?$/,Wi=22,Ut=".",it="0",Xs=";",Js=",",Ln="#";function qs(t,r,e,n,i,a,o=!1){let s="",c=!1;if(!isFinite(t))s=Pe(e,q.Infinity);else{let u=tc(t);o&&(u=ec(u));let f=r.minInt,d=r.minFrac,p=r.maxFrac;if(a){let A=a.match(Zs);if(A===null)throw new F(2306,!1);let x=A[1],T=A[3],k=A[5];x!=null&&(f=Bn(x)),T!=null&&(d=Bn(T)),k!=null?p=Bn(k):T!=null&&d>p&&(p=d)}nc(u,d,p);let h=u.digits,E=u.integerLen,_=u.exponent,v=[];for(c=h.every(A=>!A);E<f;E++)h.unshift(0);for(;E<0;E++)h.unshift(0);E>0?v=h.splice(E,h.length):(v=h,h=[0]);let y=[];for(h.length>=r.lgSize&&y.unshift(h.splice(-r.lgSize,h.length).join(""));h.length>r.gSize;)y.unshift(h.splice(-r.gSize,h.length).join(""));h.length&&y.unshift(h.join("")),s=y.join(Pe(e,n)),v.length&&(s+=Pe(e,i)+v.join("")),_&&(s+=Pe(e,q.Exponential)+"+"+_)}return t<0&&!c?s=r.negPre+s+r.negSuf:s=r.posPre+s+r.posSuf,s}function Yi(t,r,e){let n=Gi(r,Un.Decimal),i=Qs(n,Pe(r,q.MinusSign));return qs(t,i,r,q.Group,q.Decimal,e)}function Qs(t,r="-"){let e={minInt:1,minFrac:0,maxFrac:0,posPre:"",posSuf:"",negPre:"",negSuf:"",gSize:0,lgSize:0},n=t.split(Xs),i=n[0],a=n[1],o=i.indexOf(Ut)!==-1?i.split(Ut):[i.substring(0,i.lastIndexOf(it)+1),i.substring(i.lastIndexOf(it)+1)],s=o[0],c=o[1]||"";e.posPre=s.substring(0,s.indexOf(Ln));for(let f=0;f<c.length;f++){let d=c.charAt(f);d===it?e.minFrac=e.maxFrac=f+1:d===Ln?e.maxFrac=f+1:e.posSuf+=d}let u=s.split(Js);if(e.gSize=u[1]?u[1].length:0,e.lgSize=u[2]||u[1]?(u[2]||u[1]).length:0,a){let f=i.length-e.posPre.length-e.posSuf.length,d=a.indexOf(Ln);e.negPre=a.substring(0,d).replace(/'/g,""),e.negSuf=a.slice(d+f).replace(/'/g,"")}else e.negPre=r+e.posPre,e.negSuf=e.posSuf;return e}function ec(t){if(t.digits[0]===0)return t;let r=t.digits.length-t.integerLen;return t.exponent?t.exponent+=2:(r===0?t.digits.push(0,0):r===1&&t.digits.push(0),t.integerLen+=2),t}function tc(t){let r=Math.abs(t)+"",e=0,n,i,a,o,s;for((i=r.indexOf(Ut))>-1&&(r=r.replace(Ut,"")),(a=r.search(/e/i))>0?(i<0&&(i=a),i+=+r.slice(a+1),r=r.substring(0,a)):i<0&&(i=r.length),a=0;r.charAt(a)===it;a++);if(a===(s=r.length))n=[0],i=1;else{for(s--;r.charAt(s)===it;)s--;for(i-=a,n=[],o=0;a<=s;a++,o++)n[o]=Number(r.charAt(a))}return i>Wi&&(n=n.splice(0,Wi-1),e=i-1,i=1),{digits:n,exponent:e,integerLen:i}}function nc(t,r,e){if(r>e)throw new F(2307,!1);let n=t.digits,i=n.length-t.integerLen,a=Math.min(Math.max(r,i),e),o=a+t.integerLen,s=n[o];if(o>0){n.splice(Math.max(t.integerLen,o));for(let d=o;d<n.length;d++)n[d]=0}else{i=Math.max(0,i),t.integerLen=1,n.length=Math.max(1,o=a+1),n[0]=0;for(let d=1;d<o;d++)n[d]=0}if(s>=5)if(o-1<0){for(let d=0;d>o;d--)n.unshift(0),t.integerLen++;n.unshift(1),t.integerLen++}else n[o-1]++;for(;i<Math.max(0,a);i++)n.push(0);let c=a!==0,u=r+t.integerLen,f=n.reduceRight(function(d,p,h,E){return p=p+d,E[h]=p<10?p:p-10,c&&(E[h]===0&&h>=u?E.pop():c=!1),p>=10?1:0},0);f&&(n.unshift(f),t.integerLen++)}function Bn(t){let r=parseInt(t);if(isNaN(r))throw new F(2305,!1);return r}var rc=(()=>{class t{_viewContainerRef;_viewRef=null;ngTemplateOutletContext=null;ngTemplateOutlet=null;ngTemplateOutletInjector=null;injector=l(U);constructor(e){this._viewContainerRef=e}ngOnChanges(e){if(this._shouldRecreateView(e)){let n=this._viewContainerRef;if(this._viewRef&&n.remove(n.indexOf(this._viewRef)),!this.ngTemplateOutlet){this._viewRef=null;return}let i=this._createContextForwardProxy();this._viewRef=n.createEmbeddedView(this.ngTemplateOutlet,i,{injector:this._getInjector()})}}_getInjector(){return this.ngTemplateOutletInjector==="outlet"?this.injector:this.ngTemplateOutletInjector??void 0}_shouldRecreateView(e){return!!e.ngTemplateOutlet||!!e.ngTemplateOutletInjector}_createContextForwardProxy(){return new Proxy({},{set:(e,n,i)=>this.ngTemplateOutletContext?Reflect.set(this.ngTemplateOutletContext,n,i):!1,get:(e,n,i)=>{if(this.ngTemplateOutletContext)return Reflect.get(this.ngTemplateOutletContext,n,i)}})}static \u0275fac=function(n){return new(n||t)(xn(Fi))};static \u0275dir=B({type:t,selectors:[["","ngTemplateOutlet",""]],inputs:{ngTemplateOutletContext:"ngTemplateOutletContext",ngTemplateOutlet:"ngTemplateOutlet",ngTemplateOutletInjector:"ngTemplateOutletInjector"},features:[Te]})}return t})();function Ki(t,r){return new F(2100,!1)}var ic=(()=>{class t{_locale;constructor(e){this._locale=e}transform(e,n,i){if(!ac(e))return null;i||=this._locale;try{let a=oc(e);return Yi(a,i,n)}catch(a){throw Ki(t,a.message)}}static \u0275fac=function(n){return new(n||t)(xn(Pi,16))};static \u0275pipe=Fn({name:"number",type:t,pure:!0})}return t})();function ac(t){return!(t==null||t===""||t!==t)}function oc(t){if(typeof t=="string"&&!isNaN(Number(t)-parseFloat(t)))return Number(t);if(typeof t!="number")throw new F(2309,!1);return t}var sc=(()=>{class t{transform(e,n,i){if(e==null)return null;if(!(typeof e=="string"||Array.isArray(e)))throw Ki(t,e);return e.slice(n,i)}static \u0275fac=function(n){return new(n||t)};static \u0275pipe=Fn({name:"slice",type:t,pure:!1})}return t})();function at(t,r){r=encodeURIComponent(r);for(let e of t.split(";")){let n=e.indexOf("="),[i,a]=n==-1?[e,""]:[e.slice(0,n),e.slice(n+1)];if(i.trim()===r)return decodeURIComponent(a)}return null}var De=class{};var zn="browser";function Xi(t){return t===zn}var Ji=t=>t.src,dc=new C("",{factory:()=>Ji});var Zi=/^((\s*\d+w\s*(,|$)){1,})$/;var fc=[1,2],mc=640;var hc=1920,pc=1080;var vp=(()=>{class t{imageLoader=l(dc);config=gc(l(Mn));renderer=l(qe);imgElement=l(H).nativeElement;injector=l(U);destroyRef=l(In);lcpObserver;_renderedSrc=null;ngSrc;ngSrcset;sizes;width;height;decoding;loading;priority=!1;loaderParams;disableOptimizedSrcset=!1;fill=!1;placeholder;placeholderConfig;src;srcset;constructor(){this.destroyRef.onDestroy(()=>{this.renderer.removeAttribute(this.imgElement,"loading")})}ngOnInit(){Mi("NgOptimizedImage"),this.placeholder&&this.removePlaceholderOnLoad(this.imgElement),this.setHostAttributes()}setHostAttributes(){this.fill?this.sizes||="100vw":(this.setHostAttribute("width",this.width.toString()),this.setHostAttribute("height",this.height.toString())),this.setHostAttribute("loading",this.getLoadingBehavior()),this.setHostAttribute("fetchpriority",this.getFetchPriority()),this.setHostAttribute("decoding",this.getDecoding()),this.setHostAttribute("ng-img","true");let e=this.updateSrcAndSrcset();this.sizes?this.getLoadingBehavior()==="lazy"?this.setHostAttribute("sizes","auto, "+this.sizes):this.setHostAttribute("sizes",this.sizes):this.ngSrcset&&Zi.test(this.ngSrcset)&&this.getLoadingBehavior()==="lazy"&&this.setHostAttribute("sizes","auto, 100vw")}ngOnChanges(e){if(e.ngSrc&&!e.ngSrc.isFirstChange()){let n=this._renderedSrc;this.updateSrcAndSrcset(!0)}}getAspectRatio(){return this.width&&this.height&&this.height!==0?this.width/this.height:null}callImageLoader(e){let n=e;this.loaderParams&&(n.loaderParams=this.loaderParams);let i=this.getAspectRatio();return i!==null&&n.width&&(n.height=Math.round(n.width/i)),this.imageLoader(n)}getLoadingBehavior(){return!this.priority&&this.loading!==void 0?this.loading:this.priority?"eager":"lazy"}getFetchPriority(){return this.priority?"high":"auto"}getDecoding(){return this.priority?"sync":this.decoding??"auto"}getRewrittenSrc(){if(!this._renderedSrc){let e={src:this.ngSrc};this._renderedSrc=this.callImageLoader(e)}return this._renderedSrc}getRewrittenSrcset(){let e=Zi.test(this.ngSrcset);return this.ngSrcset.split(",").filter(i=>i!=="").map(i=>{i=i.trim();let a=e?parseFloat(i):parseFloat(i)*this.width;return`${this.callImageLoader({src:this.ngSrc,width:a})} ${i}`}).join(", ")}getAutomaticSrcset(){return this.sizes?this.getResponsiveSrcset():this.getFixedSrcset()}getResponsiveSrcset(){let{breakpoints:e}=this.config,n=e;return this.sizes?.trim()==="100vw"&&(n=e.filter(a=>a>=mc)),n.map(a=>`${this.callImageLoader({src:this.ngSrc,width:a})} ${a}w`).join(", ")}updateSrcAndSrcset(e=!1){e&&(this._renderedSrc=null);let n=this.getRewrittenSrc();this.setHostAttribute("src",n);let i;return this.ngSrcset?i=this.getRewrittenSrcset():this.shouldGenerateAutomaticSrcset()&&(i=this.getAutomaticSrcset()),i&&this.setHostAttribute("srcset",i),i}getFixedSrcset(){return fc.map(n=>`${this.callImageLoader({src:this.ngSrc,width:this.width*n})} ${n}x`).join(", ")}shouldGenerateAutomaticSrcset(){let e=!1;return this.sizes||(e=this.width>hc||this.height>pc),!this.disableOptimizedSrcset&&!this.srcset&&this.imageLoader!==Ji&&!e}generatePlaceholder(e){let{placeholderResolution:n}=this.config;return e===!0?`url(${this.callImageLoader({src:this.ngSrc,width:n,isPlaceholder:!0})})`:typeof e=="string"?`url(${e})`:null}shouldBlurPlaceholder(e){return!e||!e.hasOwnProperty("blur")?!0:!!e.blur}removePlaceholderOnLoad(e){let n=()=>{let o=this.injector.get(Nn);i(),a(),this.placeholder=!1,o.markForCheck()},i=this.renderer.listen(e,"load",n),a=this.renderer.listen(e,"error",n);this.destroyRef.onDestroy(()=>{i(),a()}),bc(e,n)}setHostAttribute(e,n){this.renderer.setAttribute(this.imgElement,e,n)}static \u0275fac=function(n){return new(n||t)};static \u0275dir=B({type:t,selectors:[["img","ngSrc",""]],hostVars:18,hostBindings:function(n,i){n&2&&Ni("position",i.fill?"absolute":null)("width",i.fill?"100%":null)("height",i.fill?"100%":null)("inset",i.fill?"0":null)("background-size",i.placeholder?"cover":null)("background-position",i.placeholder?"50% 50%":null)("background-repeat",i.placeholder?"no-repeat":null)("background-image",i.placeholder?i.generatePlaceholder(i.placeholder):null)("filter",i.placeholder&&i.shouldBlurPlaceholder(i.placeholderConfig)?"blur(15px)":null)},inputs:{ngSrc:[2,"ngSrc","ngSrc",vc],ngSrcset:"ngSrcset",sizes:"sizes",width:[2,"width","width",nt],height:[2,"height","height",nt],decoding:"decoding",loading:"loading",priority:[2,"priority","priority",W],loaderParams:"loaderParams",disableOptimizedSrcset:[2,"disableOptimizedSrcset","disableOptimizedSrcset",W],fill:[2,"fill","fill",W],placeholder:[2,"placeholder","placeholder",yc],placeholderConfig:"placeholderConfig",src:"src",srcset:"srcset"},features:[Te]})}return t})();function gc(t){let r={};return t.breakpoints&&(r.breakpoints=t.breakpoints.sort((e,n)=>e-n)),Object.assign({},yi,t,r)}function bc(t,r){t.complete&&t.naturalWidth&&r()}function vc(t){return typeof t=="string"?t:ue(t)}function yc(t){return typeof t=="string"&&t!=="true"&&t!=="false"&&t!==""?t:W(t)}var ot=class{_doc;constructor(r){this._doc=r}manager},zt=(()=>{class t extends ot{constructor(e){super(e)}supports(e){return!0}addEventListener(e,n,i,a){return e.addEventListener(n,i,a),()=>this.removeEventListener(e,n,i,a)}removeEventListener(e,n,i,a){return e.removeEventListener(n,i,a)}static \u0275fac=function(n){return new(n||t)(w(I))};static \u0275prov=g({token:t,factory:t.\u0275fac})}return t})(),Vt=new C(""),Wn=(()=>{class t{_zone;_plugins;_eventNameToPlugin=new Map;constructor(e,n){this._zone=n,e.forEach(o=>{o.manager=this});let i=e.filter(o=>!(o instanceof zt));this._plugins=i.slice().reverse();let a=e.find(o=>o instanceof zt);a&&this._plugins.push(a)}addEventListener(e,n,i,a){return this._findPluginFor(n).addEventListener(e,n,i,a)}getZone(){return this._zone}_findPluginFor(e){let n=this._eventNameToPlugin.get(e);if(n)return n;if(n=this._plugins.find(a=>a.supports(e)),!n)throw new F(5101,!1);return this._eventNameToPlugin.set(e,n),n}static \u0275fac=function(n){return new(n||t)(w(Vt),w(O))};static \u0275prov=g({token:t,factory:t.\u0275fac})}return t})(),$n="ng-app-id";function qi(t){for(let r of t)r.remove()}function Qi(t,r){let e=r.createElement("style");return e.textContent=t,e}function Dc(t,r,e,n){let i=t.head?.querySelectorAll(`style[${$n}="${r}"],link[${$n}="${r}"]`);if(i)for(let a of i)a.removeAttribute($n),a instanceof HTMLLinkElement?n.set(a.href.slice(a.href.lastIndexOf("/")+1),{usage:0,elements:[a]}):a.textContent&&e.set(a.textContent,{usage:0,elements:[a]})}function Vn(t,r){let e=r.createElement("link");return e.setAttribute("rel","stylesheet"),e.setAttribute("href",t),e}var Gn=(()=>{class t{doc;appId;nonce;inline=new Map;external=new Map;hosts=new Set;constructor(e,n,i,a={}){this.doc=e,this.appId=n,this.nonce=i,Dc(e,n,this.inline,this.external),this.hosts.add(e.head)}addStyles(e,n){for(let i of e)this.addUsage(i,this.inline,Qi);n?.forEach(i=>this.addUsage(i,this.external,Vn))}removeStyles(e,n){for(let i of e)this.removeUsage(i,this.inline);n?.forEach(i=>this.removeUsage(i,this.external))}addUsage(e,n,i){let a=n.get(e);a?a.usage++:n.set(e,{usage:1,elements:[...this.hosts].map(o=>this.addElement(o,i(e,this.doc)))})}removeUsage(e,n){let i=n.get(e);i&&(i.usage--,i.usage<=0&&(qi(i.elements),n.delete(e)))}ngOnDestroy(){for(let[,{elements:e}]of[...this.inline,...this.external])qi(e);this.hosts.clear()}addHost(e){this.hosts.add(e);for(let[n,{elements:i}]of this.inline)i.push(this.addElement(e,Qi(n,this.doc)));for(let[n,{elements:i}]of this.external)i.push(this.addElement(e,Vn(n,this.doc)))}removeHost(e){this.hosts.delete(e)}addElement(e,n){return this.nonce&&n.setAttribute("nonce",this.nonce),e.appendChild(n)}static \u0275fac=function(n){return new(n||t)(w(I),w(ye),w(Je,8),w(Se))};static \u0275prov=g({token:t,factory:t.\u0275fac})}return t})(),Hn={svg:"http://www.w3.org/2000/svg",xhtml:"http://www.w3.org/1999/xhtml",xlink:"http://www.w3.org/1999/xlink",xml:"http://www.w3.org/XML/1998/namespace",xmlns:"http://www.w3.org/2000/xmlns/",math:"http://www.w3.org/1998/Math/MathML"},Yn=/%COMP%/g;var ta="%COMP%",_c=`_nghost-${ta}`,Ec=`_ngcontent-${ta}`,wc=!0,Ac=new C("",{factory:()=>wc});function Cc(t){return Ec.replace(Yn,t)}function Ic(t){return _c.replace(Yn,t)}function na(t,r){return r.map(e=>e.replace(Yn,t))}var Kn=(()=>{class t{eventManager;sharedStylesHost;appId;removeStylesOnCompDestroy;doc;ngZone;nonce;tracingService;rendererByCompId=new Map;defaultRenderer;constructor(e,n,i,a,o,s,c=null,u=null){this.eventManager=e,this.sharedStylesHost=n,this.appId=i,this.removeStylesOnCompDestroy=a,this.doc=o,this.ngZone=s,this.nonce=c,this.tracingService=u,this.defaultRenderer=new st(e,o,s,this.tracingService)}createRenderer(e,n){if(!e||!n)return this.defaultRenderer;let i=this.getOrCreateRenderer(e,n);return i instanceof Ht?i.applyToHost(e):i instanceof ct&&i.applyStyles(),i}getOrCreateRenderer(e,n){let i=this.rendererByCompId,a=i.get(n.id);if(!a){let o=this.doc,s=this.ngZone,c=this.eventManager,u=this.sharedStylesHost,f=this.removeStylesOnCompDestroy,d=this.tracingService;switch(n.encapsulation){case kt.Emulated:a=new Ht(c,u,n,this.appId,f,o,s,d);break;case kt.ShadowDom:return new $t(c,e,n,o,s,this.nonce,d,u);case kt.ExperimentalIsolatedShadowDom:return new $t(c,e,n,o,s,this.nonce,d);default:a=new ct(c,u,n,f,o,s,d);break}i.set(n.id,a)}return a}ngOnDestroy(){this.rendererByCompId.clear()}componentReplaced(e){this.rendererByCompId.delete(e)}static \u0275fac=function(n){return new(n||t)(w(Wn),w(Gn),w(ye),w(Ac),w(I),w(O),w(Je),w(Nt,8))};static \u0275prov=g({token:t,factory:t.\u0275fac})}return t})(),st=class{eventManager;doc;ngZone;tracingService;data=Object.create(null);throwOnSyntheticProps=!0;constructor(r,e,n,i){this.eventManager=r,this.doc=e,this.ngZone=n,this.tracingService=i}destroy(){}destroyNode=null;createElement(r,e){return e?this.doc.createElementNS(Hn[e]||e,r):this.doc.createElement(r)}createComment(r){return this.doc.createComment(r)}createText(r){return this.doc.createTextNode(r)}appendChild(r,e){(ea(r)?r.content:r).appendChild(e)}insertBefore(r,e,n){r&&(ea(r)?r.content:r).insertBefore(e,n)}removeChild(r,e){e.remove()}selectRootElement(r,e){let n=typeof r=="string"?this.doc.querySelector(r):r;if(!n)throw new F(-5104,!1);return e||(n.textContent=""),n}parentNode(r){return r.parentNode}nextSibling(r){return r.nextSibling}setAttribute(r,e,n,i){if(i){e=i+":"+e;let a=Hn[i];a?r.setAttributeNS(a,e,n):r.setAttribute(e,n)}else r.setAttribute(e,n)}removeAttribute(r,e,n){if(n){let i=Hn[n];i?r.removeAttributeNS(i,e):r.removeAttribute(`${n}:${e}`)}else r.removeAttribute(e)}addClass(r,e){r.classList.add(e)}removeClass(r,e){r.classList.remove(e)}setStyle(r,e,n,i){i&(xe.DashCase|xe.Important)?r.style.setProperty(e,n,i&xe.Important?"important":""):r.style[e]=n}removeStyle(r,e,n){n&xe.DashCase?r.style.removeProperty(e):r.style[e]=""}setProperty(r,e,n){r!=null&&(r[e]=n)}setValue(r,e){r.nodeValue=e}listen(r,e,n,i){if(typeof r=="string"&&(r=re().getGlobalEventTarget(this.doc,r),!r))throw new F(5102,!1);let a=this.decoratePreventDefault(n);return this.tracingService?.wrapEventListener&&(a=this.tracingService.wrapEventListener(r,e,a)),this.eventManager.addEventListener(r,e,a,i)}decoratePreventDefault(r){return e=>{if(e==="__ngUnwrap__")return r;r(e)===!1&&e.preventDefault()}}};function ea(t){return t.tagName==="TEMPLATE"&&t.content!==void 0}var $t=class extends st{hostEl;sharedStylesHost;shadowRoot;constructor(r,e,n,i,a,o,s,c){super(r,i,a,s),this.hostEl=e,this.sharedStylesHost=c,this.shadowRoot=e.attachShadow({mode:"open"}),this.sharedStylesHost&&this.sharedStylesHost.addHost(this.shadowRoot);let u=n.styles;u=na(n.id,u);for(let d of u){let p=document.createElement("style");o&&p.setAttribute("nonce",o),p.textContent=d,this.shadowRoot.appendChild(p)}let f=n.getExternalStyles?.();if(f)for(let d of f){let p=Vn(d,i);o&&p.setAttribute("nonce",o),this.shadowRoot.appendChild(p)}}nodeOrShadowRoot(r){return r===this.hostEl?this.shadowRoot:r}appendChild(r,e){return super.appendChild(this.nodeOrShadowRoot(r),e)}insertBefore(r,e,n){return super.insertBefore(this.nodeOrShadowRoot(r),e,n)}removeChild(r,e){return super.removeChild(null,e)}parentNode(r){return this.nodeOrShadowRoot(super.parentNode(this.nodeOrShadowRoot(r)))}destroy(){this.sharedStylesHost&&this.sharedStylesHost.removeHost(this.shadowRoot)}},ct=class extends st{sharedStylesHost;removeStylesOnCompDestroy;styles;styleUrls;constructor(r,e,n,i,a,o,s,c){super(r,a,o,s),this.sharedStylesHost=e,this.removeStylesOnCompDestroy=i;let u=n.styles;this.styles=c?na(c,u):u,this.styleUrls=n.getExternalStyles?.(c)}applyStyles(){this.sharedStylesHost.addStyles(this.styles,this.styleUrls)}destroy(){this.removeStylesOnCompDestroy&&Si.size===0&&this.sharedStylesHost.removeStyles(this.styles,this.styleUrls)}},Ht=class extends ct{contentAttr;hostAttr;constructor(r,e,n,i,a,o,s,c){let u=i+"-"+n.id;super(r,e,n,a,o,s,c,u),this.contentAttr=Cc(u),this.hostAttr=Ic(u)}applyToHost(r){this.applyStyles(),this.setAttribute(r,this.hostAttr,"")}createElement(r,e){let n=super.createElement(r,e);return super.setAttribute(n,this.contentAttr,""),n}};var Wt=class t extends rt{supportsDOMEvents=!0;static makeCurrent(){Pn(new t)}onAndCancel(r,e,n,i){return r.addEventListener(e,n,i),()=>{r.removeEventListener(e,n,i)}}dispatchEvent(r,e){r.dispatchEvent(e)}remove(r){r.remove()}createElement(r,e){return e=e||this.getDefaultDocument(),e.createElement(r)}createHtmlDocument(){return document.implementation.createHTMLDocument("fakeTitle")}getDefaultDocument(){return document}isElementNode(r){return r.nodeType===Node.ELEMENT_NODE}isShadowRoot(r){return r instanceof DocumentFragment}getGlobalEventTarget(r,e){return e==="window"?window:e==="document"?r:e==="body"?r.body:null}getBaseHref(r){let e=Sc();return e==null?null:Mc(e)}resetBaseElement(){ut=null}getUserAgent(){return window.navigator.userAgent}getCookie(r){return at(document.cookie,r)}},ut=null;function Sc(){return ut=ut||document.head.querySelector("base"),ut?ut.getAttribute("href"):null}function Mc(t){return new URL(t,document.baseURI).pathname}var xc=(()=>{class t{build(){return new XMLHttpRequest}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac})}return t})(),ra=["alt","control","meta","shift"],Fc={"\b":"Backspace","	":"Tab","\x7F":"Delete","\x1B":"Escape",Del:"Delete",Esc:"Escape",Left:"ArrowLeft",Right:"ArrowRight",Up:"ArrowUp",Down:"ArrowDown",Menu:"ContextMenu",Scroll:"ScrollLock",Win:"OS"},Rc={alt:t=>t.altKey,control:t=>t.ctrlKey,meta:t=>t.metaKey,shift:t=>t.shiftKey},ia=(()=>{class t extends ot{constructor(e){super(e)}supports(e){return t.parseEventName(e)!=null}addEventListener(e,n,i,a){let o=t.parseEventName(n),s=t.eventCallback(o.fullKey,i,this.manager.getZone());return this.manager.getZone().runOutsideAngular(()=>re().onAndCancel(e,o.domEventName,s,a))}static parseEventName(e){let n=e.toLowerCase().split("."),i=n.shift();if(n.length===0||!(i==="keydown"||i==="keyup"))return null;let a=t._normalizeKey(n.pop()),o="",s=n.indexOf("code");if(s>-1&&(n.splice(s,1),o="code."),ra.forEach(u=>{let f=n.indexOf(u);f>-1&&(n.splice(f,1),o+=u+".")}),o+=a,n.length!=0||a.length===0)return null;let c={};return c.domEventName=i,c.fullKey=o,c}static matchEventFullKeyCode(e,n){let i=Fc[e.key]||e.key,a="";return n.indexOf("code.")>-1&&(i=e.code,a="code."),i==null||!i?!1:(i=i.toLowerCase(),i===" "?i="space":i==="."&&(i="dot"),ra.forEach(o=>{if(o!==i){let s=Rc[o];s(e)&&(a+=o+".")}}),a+=i,a===n)}static eventCallback(e,n,i){return a=>{t.matchEventFullKeyCode(a,e)&&i.runGuarded(()=>n(a))}}static _normalizeKey(e){return e==="esc"?"escape":e}static \u0275fac=function(n){return new(n||t)(w(I))};static \u0275prov=g({token:t,factory:t.\u0275fac})}return t})();async function Oc(t,r,e){let n=R({rootComponent:t},kc(r,e));return Li(n)}function kc(t,r){return{platformRef:r?.platformRef,appProviders:[...jc,...t?.providers??[]],platformProviders:Bc}}function Nc(){Wt.makeCurrent()}function Pc(){return new Tn}function Lc(){return gi(document),document}var Bc=[{provide:Se,useValue:zn},{provide:bi,useValue:Nc,multi:!0},{provide:I,useFactory:Lc}];var jc=[{provide:mi,useValue:"root"},{provide:Tn,useFactory:Pc},{provide:Vt,useClass:zt,multi:!0},{provide:Vt,useClass:ia,multi:!0},Kn,Gn,Wn,{provide:Fe,useExisting:Kn},{provide:De,useClass:xc},[]];var fe=class t{headers;normalizedNames=new Map;lazyInit;lazyUpdate=null;constructor(r){r?typeof r=="string"?this.lazyInit=()=>{this.headers=new Map,r.split(`
`).forEach(e=>{let n=e.indexOf(":");if(n>0){let i=e.slice(0,n),a=e.slice(n+1).trim();this.addHeaderEntry(i,a)}})}:typeof Headers<"u"&&r instanceof Headers?(this.headers=new Map,r.forEach((e,n)=>{this.addHeaderEntry(n,e)})):this.lazyInit=()=>{this.headers=new Map,Object.entries(r).forEach(([e,n])=>{this.setHeaderEntries(e,n)})}:this.headers=new Map}has(r){return this.init(),this.headers.has(r.toLowerCase())}get(r){this.init();let e=this.headers.get(r.toLowerCase());return e&&e.length>0?e[0]:null}keys(){return this.init(),Array.from(this.normalizedNames.values())}getAll(r){return this.init(),this.headers.get(r.toLowerCase())||null}append(r,e){return this.clone({name:r,value:e,op:"a"})}set(r,e){return this.clone({name:r,value:e,op:"s"})}delete(r,e){return this.clone({name:r,value:e,op:"d"})}maybeSetNormalizedName(r,e){this.normalizedNames.has(e)||this.normalizedNames.set(e,r)}init(){this.lazyInit&&(this.lazyInit instanceof t?this.copyFrom(this.lazyInit):this.lazyInit(),this.lazyInit=null,this.lazyUpdate&&(this.lazyUpdate.forEach(r=>this.applyUpdate(r)),this.lazyUpdate=null))}copyFrom(r){r.init(),Array.from(r.headers.keys()).forEach(e=>{this.headers.set(e,r.headers.get(e)),this.normalizedNames.set(e,r.normalizedNames.get(e))})}clone(r){let e=new t;return e.lazyInit=this.lazyInit&&this.lazyInit instanceof t?this.lazyInit:this,e.lazyUpdate=(this.lazyUpdate||[]).concat([r]),e}applyUpdate(r){let e=r.name.toLowerCase();switch(r.op){case"a":case"s":let n=r.value;if(typeof n=="string"&&(n=[n]),n.length===0)return;this.maybeSetNormalizedName(r.name,e);let i=(r.op==="a"?this.headers.get(e):void 0)||[];i.push(...n),this.headers.set(e,i);break;case"d":let a=r.value;if(!a)this.headers.delete(e),this.normalizedNames.delete(e);else{let o=this.headers.get(e);if(!o)return;o=o.filter(s=>a.indexOf(s)===-1),o.length===0?(this.headers.delete(e),this.normalizedNames.delete(e)):this.headers.set(e,o)}break}}addHeaderEntry(r,e){let n=r.toLowerCase();this.maybeSetNormalizedName(r,n),this.headers.has(n)?this.headers.get(n).push(e):this.headers.set(n,[e])}setHeaderEntries(r,e){let n=(Array.isArray(e)?e:[e]).map(a=>a.toString()),i=r.toLowerCase();this.headers.set(i,n),this.maybeSetNormalizedName(r,i)}forEach(r){this.init(),Array.from(this.normalizedNames.keys()).forEach(e=>r(this.normalizedNames.get(e),this.headers.get(e)))}};var Yt=class{map=new Map;set(r,e){return this.map.set(r,e),this}get(r){return this.map.has(r)||this.map.set(r,r.defaultValue()),this.map.get(r)}delete(r){return this.map.delete(r),this}has(r){return this.map.has(r)}keys(){return this.map.keys()}},Kt=class{encodeKey(r){return aa(r)}encodeValue(r){return aa(r)}decodeKey(r){return decodeURIComponent(r)}decodeValue(r){return decodeURIComponent(r)}};function Uc(t,r){let e=new Map;return t.length>0&&t.replace(/^\?/,"").split("&").forEach(i=>{let a=i.indexOf("="),[o,s]=a==-1?[r.decodeKey(i),""]:[r.decodeKey(i.slice(0,a)),r.decodeValue(i.slice(a+1))],c=e.get(o)||[];c.push(s),e.set(o,c)}),e}var zc=/%(\d[a-f0-9])/gi,$c={40:"@","3A":":",24:"$","2C":",","3B":";","3D":"=","3F":"?","2F":"/"};function aa(t){return encodeURIComponent(t).replace(zc,(r,e)=>$c[e]??r)}function Gt(t){return`${t}`}var ie=class t{map;encoder;updates=null;cloneFrom=null;constructor(r={}){if(this.encoder=r.encoder||new Kt,r.fromString){if(r.fromObject)throw new F(2805,!1);this.map=Uc(r.fromString,this.encoder)}else r.fromObject?(this.map=new Map,Object.keys(r.fromObject).forEach(e=>{let n=r.fromObject[e],i=Array.isArray(n)?n.map(Gt):[Gt(n)];this.map.set(e,i)})):this.map=null}has(r){return this.init(),this.map.has(r)}get(r){this.init();let e=this.map.get(r);return e?e[0]:null}getAll(r){return this.init(),this.map.get(r)||null}keys(){return this.init(),Array.from(this.map.keys())}append(r,e){return this.clone({param:r,value:e,op:"a"})}appendAll(r){let e=[];return Object.keys(r).forEach(n=>{let i=r[n];Array.isArray(i)?i.forEach(a=>{e.push({param:n,value:a,op:"a"})}):e.push({param:n,value:i,op:"a"})}),this.clone(e)}set(r,e){return this.clone({param:r,value:e,op:"s"})}delete(r,e){return this.clone({param:r,value:e,op:"d"})}toString(){return this.init(),this.keys().map(r=>{let e=this.encoder.encodeKey(r);return this.map.get(r).map(n=>e+"="+this.encoder.encodeValue(n)).join("&")}).filter(r=>r!=="").join("&")}clone(r){let e=new t({encoder:this.encoder});return e.cloneFrom=this.cloneFrom||this,e.updates=(this.updates||[]).concat(r),e}init(){this.map===null&&(this.map=new Map),this.cloneFrom!==null&&(this.cloneFrom.init(),this.cloneFrom.keys().forEach(r=>this.map.set(r,this.cloneFrom.map.get(r))),this.updates.forEach(r=>{switch(r.op){case"a":case"s":let e=(r.op==="a"?this.map.get(r.param):void 0)||[];e.push(Gt(r.value)),this.map.set(r.param,e);break;case"d":if(r.value!==void 0){let n=this.map.get(r.param)||[],i=n.indexOf(Gt(r.value));i!==-1&&n.splice(i,1),n.length>0?this.map.set(r.param,n):this.map.delete(r.param)}else{this.map.delete(r.param);break}}}),this.cloneFrom=this.updates=null)}};function Hc(t){switch(t){case"DELETE":case"GET":case"HEAD":case"OPTIONS":case"JSONP":return!1;default:return!0}}function oa(t){return typeof ArrayBuffer<"u"&&t instanceof ArrayBuffer}function sa(t){return typeof Blob<"u"&&t instanceof Blob}function ca(t){return typeof FormData<"u"&&t instanceof FormData}function Vc(t){return typeof URLSearchParams<"u"&&t instanceof URLSearchParams}var ua="Content-Type",la="Accept",da="text/plain",fa="application/json",Wc=`${fa}, ${da}, */*`,Le=class t{url;body=null;headers;context;reportProgress=!1;withCredentials=!1;credentials;keepalive=!1;cache;priority;mode;redirect;referrer;integrity;referrerPolicy;responseType="json";method;params;urlWithParams;transferCache;timeout;constructor(r,e,n,i){this.url=e,this.method=r.toUpperCase();let a;if(Hc(this.method)||i?(this.body=n!==void 0?n:null,a=i):a=n,a){if(this.reportProgress=!!a.reportProgress,this.withCredentials=!!a.withCredentials,this.keepalive=!!a.keepalive,a.responseType&&(this.responseType=a.responseType),a.headers&&(this.headers=a.headers),a.context&&(this.context=a.context),a.params&&(this.params=a.params),a.priority&&(this.priority=a.priority),a.cache&&(this.cache=a.cache),a.credentials&&(this.credentials=a.credentials),typeof a.timeout=="number"){if(a.timeout<1||!Number.isInteger(a.timeout))throw new F(2822,"");this.timeout=a.timeout}a.mode&&(this.mode=a.mode),a.redirect&&(this.redirect=a.redirect),a.integrity&&(this.integrity=a.integrity),a.referrer&&(this.referrer=a.referrer),a.referrerPolicy&&(this.referrerPolicy=a.referrerPolicy),this.transferCache=a.transferCache}if(this.headers??=new fe,this.context??=new Yt,!this.params)this.params=new ie,this.urlWithParams=e;else{let o=this.params.toString();if(o.length===0)this.urlWithParams=e;else{let s=e.indexOf("?"),c=s===-1?"?":s<e.length-1?"&":"";this.urlWithParams=e+c+o}}}serializeBody(){return this.body===null?null:typeof this.body=="string"||oa(this.body)||sa(this.body)||ca(this.body)||Vc(this.body)?this.body:this.body instanceof ie?this.body.toString():typeof this.body=="object"||typeof this.body=="boolean"||Array.isArray(this.body)?JSON.stringify(this.body):this.body.toString()}detectContentTypeHeader(){return this.body===null||ca(this.body)?null:sa(this.body)?this.body.type||null:oa(this.body)?null:typeof this.body=="string"?da:this.body instanceof ie?"application/x-www-form-urlencoded;charset=UTF-8":typeof this.body=="object"||typeof this.body=="number"||typeof this.body=="boolean"?fa:null}clone(r={}){let e=r.method||this.method,n=r.url||this.url,i=r.responseType||this.responseType,a=r.keepalive??this.keepalive,o=r.priority||this.priority,s=r.cache||this.cache,c=r.mode||this.mode,u=r.redirect||this.redirect,f=r.credentials||this.credentials,d=r.referrer||this.referrer,p=r.integrity||this.integrity,h=r.referrerPolicy||this.referrerPolicy,E=r.transferCache??this.transferCache,_=r.timeout??this.timeout,v=r.body!==void 0?r.body:this.body,y=r.withCredentials??this.withCredentials,A=r.reportProgress??this.reportProgress,x=r.headers||this.headers,T=r.params||this.params,k=r.context??this.context;return r.setHeaders!==void 0&&(x=Object.keys(r.setHeaders).reduce((ee,te)=>ee.set(te,r.setHeaders[te]),x)),r.setParams&&(T=Object.keys(r.setParams).reduce((ee,te)=>ee.set(te,r.setParams[te]),T)),new t(e,n,v,{params:T,headers:x,context:k,reportProgress:A,responseType:i,withCredentials:y,transferCache:E,keepalive:a,cache:s,priority:o,timeout:_,mode:c,redirect:u,credentials:f,referrer:d,integrity:p,referrerPolicy:h})}},_e=(function(t){return t[t.Sent=0]="Sent",t[t.UploadProgress=1]="UploadProgress",t[t.ResponseHeader=2]="ResponseHeader",t[t.DownloadProgress=3]="DownloadProgress",t[t.Response=4]="Response",t[t.User=5]="User",t})(_e||{}),je=class{headers;status;statusText;url;ok;type;redirected;responseType;constructor(r,e=200,n="OK"){this.headers=r.headers||new fe,this.status=r.status!==void 0?r.status:e,this.statusText=r.statusText||n,this.url=r.url||null,this.redirected=r.redirected,this.responseType=r.responseType,this.ok=this.status>=200&&this.status<300}},Zt=class t extends je{constructor(r={}){super(r)}type=_e.ResponseHeader;clone(r={}){return new t({headers:r.headers||this.headers,status:r.status!==void 0?r.status:this.status,statusText:r.statusText||this.statusText,url:r.url||this.url||void 0})}},lt=class t extends je{body;constructor(r={}){super(r),this.body=r.body!==void 0?r.body:null}type=_e.Response;clone(r={}){return new t({body:r.body!==void 0?r.body:this.body,headers:r.headers||this.headers,status:r.status!==void 0?r.status:this.status,statusText:r.statusText||this.statusText,url:r.url||this.url||void 0,redirected:r.redirected??this.redirected,responseType:r.responseType??this.responseType})}},Be=class extends je{name="HttpErrorResponse";message;error;ok=!1;constructor(r){super(r,0,"Unknown Error"),this.status>=200&&this.status<300?this.message=`Http failure during parsing for ${r.url||"(unknown url)"}`:this.message=`Http failure response for ${r.url||"(unknown url)"}: ${r.status} ${r.statusText}`,this.error=r.error||null}},Gc=200,Yc=204;var Kc=new C("");var Zc=/^\)\]\}',?\n/;var Xn=(()=>{class t{xhrFactory;tracingService=l(Nt,{optional:!0});constructor(e){this.xhrFactory=e}maybePropagateTrace(e){return this.tracingService?.propagate?this.tracingService.propagate(e):e}handle(e){if(e.method==="JSONP")throw new F(-2800,!1);let n=this.xhrFactory;return Ke(null).pipe(di(()=>new Tt(a=>{let o=n.build();if(o.open(e.method,e.urlWithParams),e.withCredentials&&(o.withCredentials=!0),e.headers.forEach((v,y)=>o.setRequestHeader(v,y.join(","))),e.headers.has(la)||o.setRequestHeader(la,Wc),!e.headers.has(ua)){let v=e.detectContentTypeHeader();v!==null&&o.setRequestHeader(ua,v)}if(e.timeout&&(o.timeout=e.timeout),e.responseType){let v=e.responseType.toLowerCase();o.responseType=v!=="json"?v:"text"}let s=e.serializeBody(),c=null,u=()=>{if(c!==null)return c;let v=o.statusText||"OK",y=new fe(o.getAllResponseHeaders()),A=o.responseURL||e.url;return c=new Zt({headers:y,status:o.status,statusText:v,url:A}),c},f=this.maybePropagateTrace(()=>{let{headers:v,status:y,statusText:A,url:x}=u(),T=null;y!==Yc&&(T=typeof o.response>"u"?o.responseText:o.response),y===0&&(y=T?Gc:0);let k=y>=200&&y<300;if(e.responseType==="json"&&typeof T=="string"){let ee=T;T=T.replace(Zc,"");try{T=T!==""?JSON.parse(T):null}catch(te){T=ee,k&&(k=!1,T={error:te,text:T})}}k?(a.next(new lt({body:T,headers:v,status:y,statusText:A,url:x||void 0})),a.complete()):a.error(new Be({error:T,headers:v,status:y,statusText:A,url:x||void 0}))}),d=this.maybePropagateTrace(v=>{let{url:y}=u(),A=new Be({error:v,status:o.status||0,statusText:o.statusText||"Unknown Error",url:y||void 0});a.error(A)}),p=d;e.timeout&&(p=this.maybePropagateTrace(v=>{let{url:y}=u(),A=new Be({error:new DOMException("Request timed out","TimeoutError"),status:o.status||0,statusText:o.statusText||"Request timeout",url:y||void 0});a.error(A)}));let h=!1,E=this.maybePropagateTrace(v=>{h||(a.next(u()),h=!0);let y={type:_e.DownloadProgress,loaded:v.loaded};v.lengthComputable&&(y.total=v.total),e.responseType==="text"&&o.responseText&&(y.partialText=o.responseText),a.next(y)}),_=this.maybePropagateTrace(v=>{let y={type:_e.UploadProgress,loaded:v.loaded};v.lengthComputable&&(y.total=v.total),a.next(y)});return o.addEventListener("load",f),o.addEventListener("error",d),o.addEventListener("timeout",p),o.addEventListener("abort",d),e.reportProgress&&(o.addEventListener("progress",E),s!==null&&o.upload&&o.upload.addEventListener("progress",_)),o.send(s),a.next({type:_e.Sent}),()=>{o.removeEventListener("error",d),o.removeEventListener("abort",d),o.removeEventListener("load",f),o.removeEventListener("timeout",p),e.reportProgress&&(o.removeEventListener("progress",E),s!==null&&o.upload&&o.upload.removeEventListener("progress",_)),o.readyState!==o.DONE&&o.abort()}})))}static \u0275fac=function(n){return new(n||t)(w(De))};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();function Xc(t,r){return r(t)}function Jc(t,r,e){return(n,i)=>hi(e,()=>r(n,a=>t(a,i)))}var Jn=new C("",{factory:()=>[]}),ma=new C(""),ha=new C("",{factory:()=>!0});var qn=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:function(n){let i=null;return n?i=new(n||t):i=w(Xn),i},providedIn:"root"})}return t})();var Xt=(()=>{class t{backend;injector;chain=null;pendingTasks=l(pi);contributeToStability=l(ha);constructor(e,n){this.backend=e,this.injector=n}handle(e){if(this.chain===null){let n=Array.from(new Set([...this.injector.get(Jn),...this.injector.get(ma,[])]));this.chain=n.reduceRight((i,a)=>Jc(i,a,this.injector),Xc)}if(this.contributeToStability){let n=this.pendingTasks.add();return this.chain(e,i=>this.backend.handle(i)).pipe(ui(n))}else return this.chain(e,n=>this.backend.handle(n))}static \u0275fac=function(n){return new(n||t)(w(qn),w(Ze))};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})(),Qn=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:function(n){let i=null;return n?i=new(n||t):i=w(Xt),i},providedIn:"root"})}return t})();function Zn(t,r){return{body:r,headers:t.headers,context:t.context,observe:t.observe,params:t.params,reportProgress:t.reportProgress,responseType:t.responseType,withCredentials:t.withCredentials,credentials:t.credentials,transferCache:t.transferCache,timeout:t.timeout,keepalive:t.keepalive,priority:t.priority,cache:t.cache,mode:t.mode,redirect:t.redirect,integrity:t.integrity,referrer:t.referrer,referrerPolicy:t.referrerPolicy}}var pa=(()=>{class t{handler;constructor(e){this.handler=e}request(e,n,i={}){let a;if(e instanceof Le)a=e;else{let c;i.headers instanceof fe?c=i.headers:c=new fe(i.headers);let u;i.params&&(i.params instanceof ie?u=i.params:u=new ie({fromObject:i.params})),a=new Le(e,n,i.body!==void 0?i.body:null,{headers:c,context:i.context,params:u,reportProgress:i.reportProgress,responseType:i.responseType||"json",withCredentials:i.withCredentials,transferCache:i.transferCache,keepalive:i.keepalive,priority:i.priority,cache:i.cache,mode:i.mode,redirect:i.redirect,credentials:i.credentials,referrer:i.referrer,referrerPolicy:i.referrerPolicy,integrity:i.integrity,timeout:i.timeout})}let o=Ke(a).pipe(oi(c=>this.handler.handle(c)));if(e instanceof Le||i.observe==="events")return o;let s=o.pipe(St(c=>c instanceof lt));switch(i.observe||"body"){case"body":switch(a.responseType){case"arraybuffer":return s.pipe(J(c=>{if(c.body!==null&&!(c.body instanceof ArrayBuffer))throw new F(2806,!1);return c.body}));case"blob":return s.pipe(J(c=>{if(c.body!==null&&!(c.body instanceof Blob))throw new F(2807,!1);return c.body}));case"text":return s.pipe(J(c=>{if(c.body!==null&&typeof c.body!="string")throw new F(2808,!1);return c.body}));default:return s.pipe(J(c=>c.body))}case"response":return s;default:throw new F(2809,!1)}}delete(e,n={}){return this.request("DELETE",e,n)}get(e,n={}){return this.request("GET",e,n)}head(e,n={}){return this.request("HEAD",e,n)}jsonp(e,n){return this.request("JSONP",e,{params:new ie().append(n,"JSONP_CALLBACK"),observe:"body",responseType:"json"})}options(e,n={}){return this.request("OPTIONS",e,n)}patch(e,n,i={}){return this.request("PATCH",e,Zn(i,n))}post(e,n,i={}){return this.request("POST",e,Zn(i,n))}put(e,n,i={}){return this.request("PUT",e,Zn(i,n))}static \u0275fac=function(n){return new(n||t)(w(Qn))};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();var qc=new C("",{factory:()=>!0}),Qc="XSRF-TOKEN",eu=new C("",{factory:()=>Qc}),tu="X-XSRF-TOKEN",nu=new C("",{factory:()=>tu}),ru=(()=>{class t{cookieName=l(eu);doc=l(I);lastCookieString="";lastToken=null;parseCount=0;getToken(){let e=this.doc.cookie||"";return e!==this.lastCookieString&&(this.parseCount++,this.lastToken=at(e,this.cookieName),this.lastCookieString=e),this.lastToken}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})(),ga=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:function(n){let i=null;return n?i=new(n||t):i=w(ru),i},providedIn:"root"})}return t})();function iu(t,r){if(!l(qc)||t.method==="GET"||t.method==="HEAD")return r(t);try{let i=l(Ne).href,{origin:a}=new URL(i),{origin:o}=new URL(t.url,a);if(a!==o)return r(t)}catch{return r(t)}let e=l(ga).getToken(),n=l(nu);return e!=null&&!t.headers.has(n)&&(t=t.clone({headers:t.headers.set(n,e)})),r(t)}var er=(function(t){return t[t.Interceptors=0]="Interceptors",t[t.LegacyInterceptors=1]="LegacyInterceptors",t[t.CustomXsrfConfiguration=2]="CustomXsrfConfiguration",t[t.NoXsrfProtection=3]="NoXsrfProtection",t[t.JsonpSupport=4]="JsonpSupport",t[t.RequestsMadeViaParent=5]="RequestsMadeViaParent",t[t.Fetch=6]="Fetch",t})(er||{});function au(t,r){return{\u0275kind:t,\u0275providers:r}}function ou(...t){let r=[pa,Xt,{provide:Qn,useExisting:Xt},{provide:qn,useFactory:()=>l(Kc,{optional:!0})??l(Xn)},{provide:Jn,useValue:iu,multi:!0}];for(let e of t)r.push(...e.\u0275providers);return Cn(r)}function su(t){return au(er.Interceptors,t.map(r=>({provide:Jn,useValue:r,multi:!0})))}var Yg=(()=>{class t{_doc;constructor(e){this._doc=e}getTitle(){return this._doc.title}setTitle(e){this._doc.title=e||""}static \u0275fac=function(n){return new(n||t)(w(I))};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();var dt=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:function(n){let i=null;return n?i=new(n||t):i=w(cu),i},providedIn:"root"})}return t})(),cu=(()=>{class t extends dt{_doc;constructor(e){super(),this._doc=e}sanitize(e,n){if(n==null)return null;switch(e){case ne.NONE:return n;case ne.HTML:return Me(n,"HTML")?ue(n):Ii(this._doc,String(n)).toString();case ne.STYLE:return Me(n,"Style")?ue(n):n;case ne.SCRIPT:if(Me(n,"Script"))return ue(n);throw new F(5200,!1);case ne.URL:return Me(n,"URL")?ue(n):Ci(String(n));case ne.RESOURCE_URL:if(Me(n,"ResourceURL"))return ue(n);throw new F(5201,!1);default:throw new F(5202,!1)}}bypassSecurityTrustHtml(e){return Di(e)}bypassSecurityTrustStyle(e){return _i(e)}bypassSecurityTrustScript(e){return Ei(e)}bypassSecurityTrustUrl(e){return wi(e)}bypassSecurityTrustResourceUrl(e){return Ai(e)}static \u0275fac=function(n){return new(n||t)(w(I))};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();function cr(t,r){(r==null||r>t.length)&&(r=t.length);for(var e=0,n=Array(r);e<r;e++)n[e]=t[e];return n}function uu(t){if(Array.isArray(t))return t}function lu(t){if(Array.isArray(t))return cr(t)}function du(t,r){if(!(t instanceof r))throw new TypeError("Cannot call a class as a function")}function ba(t,r){for(var e=0;e<r.length;e++){var n=r[e];n.enumerable=n.enumerable||!1,n.configurable=!0,"value"in n&&(n.writable=!0),Object.defineProperty(t,Za(n.key),n)}}function fu(t,r,e){return r&&ba(t.prototype,r),e&&ba(t,e),Object.defineProperty(t,"prototype",{writable:!1}),t}function Qt(t,r){var e=typeof Symbol<"u"&&t[Symbol.iterator]||t["@@iterator"];if(!e){if(Array.isArray(t)||(e=wr(t))||r&&t&&typeof t.length=="number"){e&&(t=e);var n=0,i=function(){};return{s:i,n:function(){return n>=t.length?{done:!0}:{done:!1,value:t[n++]}},e:function(c){throw c},f:i}}throw new TypeError(`Invalid attempt to iterate non-iterable instance.
In order to be iterable, non-array objects must have a [Symbol.iterator]() method.`)}var a,o=!0,s=!1;return{s:function(){e=e.call(t)},n:function(){var c=e.next();return o=c.done,c},e:function(c){s=!0,a=c},f:function(){try{o||e.return==null||e.return()}finally{if(s)throw a}}}}function D(t,r,e){return(r=Za(r))in t?Object.defineProperty(t,r,{value:e,enumerable:!0,configurable:!0,writable:!0}):t[r]=e,t}function mu(t){if(typeof Symbol<"u"&&t[Symbol.iterator]!=null||t["@@iterator"]!=null)return Array.from(t)}function hu(t,r){var e=t==null?null:typeof Symbol<"u"&&t[Symbol.iterator]||t["@@iterator"];if(e!=null){var n,i,a,o,s=[],c=!0,u=!1;try{if(a=(e=e.call(t)).next,r===0){if(Object(e)!==e)return;c=!1}else for(;!(c=(n=a.call(e)).done)&&(s.push(n.value),s.length!==r);c=!0);}catch(f){u=!0,i=f}finally{try{if(!c&&e.return!=null&&(o=e.return(),Object(o)!==o))return}finally{if(u)throw i}}return s}}function pu(){throw new TypeError(`Invalid attempt to destructure non-iterable instance.
In order to be iterable, non-array objects must have a [Symbol.iterator]() method.`)}function gu(){throw new TypeError(`Invalid attempt to spread non-iterable instance.
In order to be iterable, non-array objects must have a [Symbol.iterator]() method.`)}function va(t,r){var e=Object.keys(t);if(Object.getOwnPropertySymbols){var n=Object.getOwnPropertySymbols(t);r&&(n=n.filter(function(i){return Object.getOwnPropertyDescriptor(t,i).enumerable})),e.push.apply(e,n)}return e}function m(t){for(var r=1;r<arguments.length;r++){var e=arguments[r]!=null?arguments[r]:{};r%2?va(Object(e),!0).forEach(function(n){D(t,n,e[n])}):Object.getOwnPropertyDescriptors?Object.defineProperties(t,Object.getOwnPropertyDescriptors(e)):va(Object(e)).forEach(function(n){Object.defineProperty(t,n,Object.getOwnPropertyDescriptor(e,n))})}return t}function on(t,r){return uu(t)||hu(t,r)||wr(t,r)||pu()}function Z(t){return lu(t)||mu(t)||wr(t)||gu()}function bu(t,r){if(typeof t!="object"||!t)return t;var e=t[Symbol.toPrimitive];if(e!==void 0){var n=e.call(t,r||"default");if(typeof n!="object")return n;throw new TypeError("@@toPrimitive must return a primitive value.")}return(r==="string"?String:Number)(t)}function Za(t){var r=bu(t,"string");return typeof r=="symbol"?r:r+""}function nn(t){"@babel/helpers - typeof";return nn=typeof Symbol=="function"&&typeof Symbol.iterator=="symbol"?function(r){return typeof r}:function(r){return r&&typeof Symbol=="function"&&r.constructor===Symbol&&r!==Symbol.prototype?"symbol":typeof r},nn(t)}function wr(t,r){if(t){if(typeof t=="string")return cr(t,r);var e={}.toString.call(t).slice(8,-1);return e==="Object"&&t.constructor&&(e=t.constructor.name),e==="Map"||e==="Set"?Array.from(t):e==="Arguments"||/^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(e)?cr(t,r):void 0}}var ya=function(){},Ar={},Xa={},Ja=null,qa={mark:ya,measure:ya};try{typeof window<"u"&&(Ar=window),typeof document<"u"&&(Xa=document),typeof MutationObserver<"u"&&(Ja=MutationObserver),typeof performance<"u"&&(qa=performance)}catch{}var vu=Ar.navigator||{},Da=vu.userAgent,_a=Da===void 0?"":Da,he=Ar,M=Xa,Ea=Ja,Jt=qa,Xg=!!he.document,se=!!M.documentElement&&!!M.head&&typeof M.addEventListener=="function"&&typeof M.createElement=="function",Qa=~_a.indexOf("MSIE")||~_a.indexOf("Trident/"),tr,yu=/fa(k|kd|s|r|l|t|d|dr|dl|dt|b|slr|slpr|wsb|tl|ns|nds|es|gt|jr|jfr|jdr|usb|ufsb|udsb|cr|ss|sr|sl|st|sds|sdr|sdl|sdt)?[\-\ ]/,Du=/Font ?Awesome ?([567 ]*)(Solid|Regular|Light|Thin|Duotone|Brands|Free|Pro|Sharp Duotone|Sharp|Kit|Notdog Duo|Notdog|Chisel|Etch|Graphite|Thumbprint|Jelly Fill|Jelly Duo|Jelly|Utility|Utility Fill|Utility Duo|Slab Press|Slab|Whiteboard)?.*/i,eo={classic:{fa:"solid",fas:"solid","fa-solid":"solid",far:"regular","fa-regular":"regular",fal:"light","fa-light":"light",fat:"thin","fa-thin":"thin",fab:"brands","fa-brands":"brands"},duotone:{fa:"solid",fad:"solid","fa-solid":"solid","fa-duotone":"solid",fadr:"regular","fa-regular":"regular",fadl:"light","fa-light":"light",fadt:"thin","fa-thin":"thin"},sharp:{fa:"solid",fass:"solid","fa-solid":"solid",fasr:"regular","fa-regular":"regular",fasl:"light","fa-light":"light",fast:"thin","fa-thin":"thin"},"sharp-duotone":{fa:"solid",fasds:"solid","fa-solid":"solid",fasdr:"regular","fa-regular":"regular",fasdl:"light","fa-light":"light",fasdt:"thin","fa-thin":"thin"},slab:{"fa-regular":"regular",faslr:"regular"},"slab-press":{"fa-regular":"regular",faslpr:"regular"},thumbprint:{"fa-light":"light",fatl:"light"},whiteboard:{"fa-semibold":"semibold",fawsb:"semibold"},notdog:{"fa-solid":"solid",fans:"solid"},"notdog-duo":{"fa-solid":"solid",fands:"solid"},etch:{"fa-solid":"solid",faes:"solid"},graphite:{"fa-thin":"thin",fagt:"thin"},jelly:{"fa-regular":"regular",fajr:"regular"},"jelly-fill":{"fa-regular":"regular",fajfr:"regular"},"jelly-duo":{"fa-regular":"regular",fajdr:"regular"},chisel:{"fa-regular":"regular",facr:"regular"},utility:{"fa-semibold":"semibold",fausb:"semibold"},"utility-duo":{"fa-semibold":"semibold",faudsb:"semibold"},"utility-fill":{"fa-semibold":"semibold",faufsb:"semibold"}},_u={GROUP:"duotone-group",SWAP_OPACITY:"swap-opacity",PRIMARY:"primary",SECONDARY:"secondary"},to=["fa-classic","fa-duotone","fa-sharp","fa-sharp-duotone","fa-thumbprint","fa-whiteboard","fa-notdog","fa-notdog-duo","fa-chisel","fa-etch","fa-graphite","fa-jelly","fa-jelly-fill","fa-jelly-duo","fa-slab","fa-slab-press","fa-utility","fa-utility-duo","fa-utility-fill"],N="classic",gt="duotone",no="sharp",ro="sharp-duotone",io="chisel",ao="etch",oo="graphite",so="jelly",co="jelly-duo",uo="jelly-fill",lo="notdog",fo="notdog-duo",mo="slab",ho="slab-press",po="thumbprint",go="utility",bo="utility-duo",vo="utility-fill",yo="whiteboard",Eu="Classic",wu="Duotone",Au="Sharp",Cu="Sharp Duotone",Iu="Chisel",Tu="Etch",Su="Graphite",Mu="Jelly",xu="Jelly Duo",Fu="Jelly Fill",Ru="Notdog",Ou="Notdog Duo",ku="Slab",Nu="Slab Press",Pu="Thumbprint",Lu="Utility",Bu="Utility Duo",ju="Utility Fill",Uu="Whiteboard",Do=[N,gt,no,ro,io,ao,oo,so,co,uo,lo,fo,mo,ho,po,go,bo,vo,yo],Jg=(tr={},D(D(D(D(D(D(D(D(D(D(tr,N,Eu),gt,wu),no,Au),ro,Cu),io,Iu),ao,Tu),oo,Su),so,Mu),co,xu),uo,Fu),D(D(D(D(D(D(D(D(D(tr,lo,Ru),fo,Ou),mo,ku),ho,Nu),po,Pu),go,Lu),bo,Bu),vo,ju),yo,Uu)),zu={classic:{900:"fas",400:"far",normal:"far",300:"fal",100:"fat"},duotone:{900:"fad",400:"fadr",300:"fadl",100:"fadt"},sharp:{900:"fass",400:"fasr",300:"fasl",100:"fast"},"sharp-duotone":{900:"fasds",400:"fasdr",300:"fasdl",100:"fasdt"},slab:{400:"faslr"},"slab-press":{400:"faslpr"},whiteboard:{600:"fawsb"},thumbprint:{300:"fatl"},notdog:{900:"fans"},"notdog-duo":{900:"fands"},etch:{900:"faes"},graphite:{100:"fagt"},chisel:{400:"facr"},jelly:{400:"fajr"},"jelly-fill":{400:"fajfr"},"jelly-duo":{400:"fajdr"},utility:{600:"fausb"},"utility-duo":{600:"faudsb"},"utility-fill":{600:"faufsb"}},$u={"Font Awesome 7 Free":{900:"fas",400:"far"},"Font Awesome 7 Pro":{900:"fas",400:"far",normal:"far",300:"fal",100:"fat"},"Font Awesome 7 Brands":{400:"fab",normal:"fab"},"Font Awesome 7 Duotone":{900:"fad",400:"fadr",normal:"fadr",300:"fadl",100:"fadt"},"Font Awesome 7 Sharp":{900:"fass",400:"fasr",normal:"fasr",300:"fasl",100:"fast"},"Font Awesome 7 Sharp Duotone":{900:"fasds",400:"fasdr",normal:"fasdr",300:"fasdl",100:"fasdt"},"Font Awesome 7 Jelly":{400:"fajr",normal:"fajr"},"Font Awesome 7 Jelly Fill":{400:"fajfr",normal:"fajfr"},"Font Awesome 7 Jelly Duo":{400:"fajdr",normal:"fajdr"},"Font Awesome 7 Slab":{400:"faslr",normal:"faslr"},"Font Awesome 7 Slab Press":{400:"faslpr",normal:"faslpr"},"Font Awesome 7 Thumbprint":{300:"fatl",normal:"fatl"},"Font Awesome 7 Notdog":{900:"fans",normal:"fans"},"Font Awesome 7 Notdog Duo":{900:"fands",normal:"fands"},"Font Awesome 7 Etch":{900:"faes",normal:"faes"},"Font Awesome 7 Graphite":{100:"fagt",normal:"fagt"},"Font Awesome 7 Chisel":{400:"facr",normal:"facr"},"Font Awesome 7 Whiteboard":{600:"fawsb",normal:"fawsb"},"Font Awesome 7 Utility":{600:"fausb",normal:"fausb"},"Font Awesome 7 Utility Duo":{600:"faudsb",normal:"faudsb"},"Font Awesome 7 Utility Fill":{600:"faufsb",normal:"faufsb"}},Hu=new Map([["classic",{defaultShortPrefixId:"fas",defaultStyleId:"solid",styleIds:["solid","regular","light","thin","brands"],futureStyleIds:[],defaultFontWeight:900}],["duotone",{defaultShortPrefixId:"fad",defaultStyleId:"solid",styleIds:["solid","regular","light","thin"],futureStyleIds:[],defaultFontWeight:900}],["sharp",{defaultShortPrefixId:"fass",defaultStyleId:"solid",styleIds:["solid","regular","light","thin"],futureStyleIds:[],defaultFontWeight:900}],["sharp-duotone",{defaultShortPrefixId:"fasds",defaultStyleId:"solid",styleIds:["solid","regular","light","thin"],futureStyleIds:[],defaultFontWeight:900}],["chisel",{defaultShortPrefixId:"facr",defaultStyleId:"regular",styleIds:["regular"],futureStyleIds:[],defaultFontWeight:400}],["etch",{defaultShortPrefixId:"faes",defaultStyleId:"solid",styleIds:["solid"],futureStyleIds:[],defaultFontWeight:900}],["graphite",{defaultShortPrefixId:"fagt",defaultStyleId:"thin",styleIds:["thin"],futureStyleIds:[],defaultFontWeight:100}],["jelly",{defaultShortPrefixId:"fajr",defaultStyleId:"regular",styleIds:["regular"],futureStyleIds:[],defaultFontWeight:400}],["jelly-duo",{defaultShortPrefixId:"fajdr",defaultStyleId:"regular",styleIds:["regular"],futureStyleIds:[],defaultFontWeight:400}],["jelly-fill",{defaultShortPrefixId:"fajfr",defaultStyleId:"regular",styleIds:["regular"],futureStyleIds:[],defaultFontWeight:400}],["notdog",{defaultShortPrefixId:"fans",defaultStyleId:"solid",styleIds:["solid"],futureStyleIds:[],defaultFontWeight:900}],["notdog-duo",{defaultShortPrefixId:"fands",defaultStyleId:"solid",styleIds:["solid"],futureStyleIds:[],defaultFontWeight:900}],["slab",{defaultShortPrefixId:"faslr",defaultStyleId:"regular",styleIds:["regular"],futureStyleIds:[],defaultFontWeight:400}],["slab-press",{defaultShortPrefixId:"faslpr",defaultStyleId:"regular",styleIds:["regular"],futureStyleIds:[],defaultFontWeight:400}],["thumbprint",{defaultShortPrefixId:"fatl",defaultStyleId:"light",styleIds:["light"],futureStyleIds:[],defaultFontWeight:300}],["utility",{defaultShortPrefixId:"fausb",defaultStyleId:"semibold",styleIds:["semibold"],futureStyleIds:[],defaultFontWeight:600}],["utility-duo",{defaultShortPrefixId:"faudsb",defaultStyleId:"semibold",styleIds:["semibold"],futureStyleIds:[],defaultFontWeight:600}],["utility-fill",{defaultShortPrefixId:"faufsb",defaultStyleId:"semibold",styleIds:["semibold"],futureStyleIds:[],defaultFontWeight:600}],["whiteboard",{defaultShortPrefixId:"fawsb",defaultStyleId:"semibold",styleIds:["semibold"],futureStyleIds:[],defaultFontWeight:600}]]),Vu={chisel:{regular:"facr"},classic:{brands:"fab",light:"fal",regular:"far",solid:"fas",thin:"fat"},duotone:{light:"fadl",regular:"fadr",solid:"fad",thin:"fadt"},etch:{solid:"faes"},graphite:{thin:"fagt"},jelly:{regular:"fajr"},"jelly-duo":{regular:"fajdr"},"jelly-fill":{regular:"fajfr"},notdog:{solid:"fans"},"notdog-duo":{solid:"fands"},sharp:{light:"fasl",regular:"fasr",solid:"fass",thin:"fast"},"sharp-duotone":{light:"fasdl",regular:"fasdr",solid:"fasds",thin:"fasdt"},slab:{regular:"faslr"},"slab-press":{regular:"faslpr"},thumbprint:{light:"fatl"},utility:{semibold:"fausb"},"utility-duo":{semibold:"faudsb"},"utility-fill":{semibold:"faufsb"},whiteboard:{semibold:"fawsb"}},_o=["fak","fa-kit","fakd","fa-kit-duotone"],wa={kit:{fak:"kit","fa-kit":"kit"},"kit-duotone":{fakd:"kit-duotone","fa-kit-duotone":"kit-duotone"}},Wu=["kit"],Gu="kit",Yu="kit-duotone",Ku="Kit",Zu="Kit Duotone",qg=D(D({},Gu,Ku),Yu,Zu),Xu={kit:{"fa-kit":"fak"},"kit-duotone":{"fa-kit-duotone":"fakd"}},Ju={"Font Awesome Kit":{400:"fak",normal:"fak"},"Font Awesome Kit Duotone":{400:"fakd",normal:"fakd"}},qu={kit:{fak:"fa-kit"},"kit-duotone":{fakd:"fa-kit-duotone"}},Aa={kit:{kit:"fak"},"kit-duotone":{"kit-duotone":"fakd"}},nr,qt={GROUP:"duotone-group",SWAP_OPACITY:"swap-opacity",PRIMARY:"primary",SECONDARY:"secondary"},Qu=["fa-classic","fa-duotone","fa-sharp","fa-sharp-duotone","fa-thumbprint","fa-whiteboard","fa-notdog","fa-notdog-duo","fa-chisel","fa-etch","fa-graphite","fa-jelly","fa-jelly-fill","fa-jelly-duo","fa-slab","fa-slab-press","fa-utility","fa-utility-duo","fa-utility-fill"],el="classic",tl="duotone",nl="sharp",rl="sharp-duotone",il="chisel",al="etch",ol="graphite",sl="jelly",cl="jelly-duo",ul="jelly-fill",ll="notdog",dl="notdog-duo",fl="slab",ml="slab-press",hl="thumbprint",pl="utility",gl="utility-duo",bl="utility-fill",vl="whiteboard",yl="Classic",Dl="Duotone",_l="Sharp",El="Sharp Duotone",wl="Chisel",Al="Etch",Cl="Graphite",Il="Jelly",Tl="Jelly Duo",Sl="Jelly Fill",Ml="Notdog",xl="Notdog Duo",Fl="Slab",Rl="Slab Press",Ol="Thumbprint",kl="Utility",Nl="Utility Duo",Pl="Utility Fill",Ll="Whiteboard",Qg=(nr={},D(D(D(D(D(D(D(D(D(D(nr,el,yl),tl,Dl),nl,_l),rl,El),il,wl),al,Al),ol,Cl),sl,Il),cl,Tl),ul,Sl),D(D(D(D(D(D(D(D(D(nr,ll,Ml),dl,xl),fl,Fl),ml,Rl),hl,Ol),pl,kl),gl,Nl),bl,Pl),vl,Ll)),Bl="kit",jl="kit-duotone",Ul="Kit",zl="Kit Duotone",eb=D(D({},Bl,Ul),jl,zl),$l={classic:{"fa-brands":"fab","fa-duotone":"fad","fa-light":"fal","fa-regular":"far","fa-solid":"fas","fa-thin":"fat"},duotone:{"fa-regular":"fadr","fa-light":"fadl","fa-thin":"fadt"},sharp:{"fa-solid":"fass","fa-regular":"fasr","fa-light":"fasl","fa-thin":"fast"},"sharp-duotone":{"fa-solid":"fasds","fa-regular":"fasdr","fa-light":"fasdl","fa-thin":"fasdt"},slab:{"fa-regular":"faslr"},"slab-press":{"fa-regular":"faslpr"},whiteboard:{"fa-semibold":"fawsb"},thumbprint:{"fa-light":"fatl"},notdog:{"fa-solid":"fans"},"notdog-duo":{"fa-solid":"fands"},etch:{"fa-solid":"faes"},graphite:{"fa-thin":"fagt"},jelly:{"fa-regular":"fajr"},"jelly-fill":{"fa-regular":"fajfr"},"jelly-duo":{"fa-regular":"fajdr"},chisel:{"fa-regular":"facr"},utility:{"fa-semibold":"fausb"},"utility-duo":{"fa-semibold":"faudsb"},"utility-fill":{"fa-semibold":"faufsb"}},Hl={classic:["fas","far","fal","fat","fad"],duotone:["fadr","fadl","fadt"],sharp:["fass","fasr","fasl","fast"],"sharp-duotone":["fasds","fasdr","fasdl","fasdt"],slab:["faslr"],"slab-press":["faslpr"],whiteboard:["fawsb"],thumbprint:["fatl"],notdog:["fans"],"notdog-duo":["fands"],etch:["faes"],graphite:["fagt"],jelly:["fajr"],"jelly-fill":["fajfr"],"jelly-duo":["fajdr"],chisel:["facr"],utility:["fausb"],"utility-duo":["faudsb"],"utility-fill":["faufsb"]},ur={classic:{fab:"fa-brands",fad:"fa-duotone",fal:"fa-light",far:"fa-regular",fas:"fa-solid",fat:"fa-thin"},duotone:{fadr:"fa-regular",fadl:"fa-light",fadt:"fa-thin"},sharp:{fass:"fa-solid",fasr:"fa-regular",fasl:"fa-light",fast:"fa-thin"},"sharp-duotone":{fasds:"fa-solid",fasdr:"fa-regular",fasdl:"fa-light",fasdt:"fa-thin"},slab:{faslr:"fa-regular"},"slab-press":{faslpr:"fa-regular"},whiteboard:{fawsb:"fa-semibold"},thumbprint:{fatl:"fa-light"},notdog:{fans:"fa-solid"},"notdog-duo":{fands:"fa-solid"},etch:{faes:"fa-solid"},graphite:{fagt:"fa-thin"},jelly:{fajr:"fa-regular"},"jelly-fill":{fajfr:"fa-regular"},"jelly-duo":{fajdr:"fa-regular"},chisel:{facr:"fa-regular"},utility:{fausb:"fa-semibold"},"utility-duo":{faudsb:"fa-semibold"},"utility-fill":{faufsb:"fa-semibold"}},Vl=["fa-solid","fa-regular","fa-light","fa-thin","fa-duotone","fa-brands","fa-semibold"],Eo=["fa","fas","far","fal","fat","fad","fadr","fadl","fadt","fab","fass","fasr","fasl","fast","fasds","fasdr","fasdl","fasdt","faslr","faslpr","fawsb","fatl","fans","fands","faes","fagt","fajr","fajfr","fajdr","facr","fausb","faudsb","faufsb"].concat(Qu,Vl),Wl=["solid","regular","light","thin","duotone","brands","semibold"],wo=[1,2,3,4,5,6,7,8,9,10],Gl=wo.concat([11,12,13,14,15,16,17,18,19,20]),Yl=["aw","fw","pull-left","pull-right"],Kl=[].concat(Z(Object.keys(Hl)),Wl,Yl,["2xs","xs","sm","lg","xl","2xl","beat","border","fade","beat-fade","bounce","flip-both","flip-horizontal","flip-vertical","flip","inverse","layers","layers-bottom-left","layers-bottom-right","layers-counter","layers-text","layers-top-left","layers-top-right","li","pull-end","pull-start","pulse","rotate-180","rotate-270","rotate-90","rotate-by","shake","spin-pulse","spin-reverse","spin","stack-1x","stack-2x","stack","ul","width-auto","width-fixed",qt.GROUP,qt.SWAP_OPACITY,qt.PRIMARY,qt.SECONDARY]).concat(wo.map(function(t){return"".concat(t,"x")})).concat(Gl.map(function(t){return"w-".concat(t)})),Zl={"Font Awesome 5 Free":{900:"fas",400:"far"},"Font Awesome 5 Pro":{900:"fas",400:"far",normal:"far",300:"fal"},"Font Awesome 5 Brands":{400:"fab",normal:"fab"},"Font Awesome 5 Duotone":{900:"fad"}},ae="___FONT_AWESOME___",lr=16,Ao="fa",Co="svg-inline--fa",we="data-fa-i2svg",dr="data-fa-pseudo-element",Xl="data-fa-pseudo-element-pending",Cr="data-prefix",Ir="data-icon",Ca="fontawesome-i2svg",Jl="async",ql=["HTML","HEAD","STYLE","SCRIPT"],Io=["::before","::after",":before",":after"],To=(function(){try{return!0}catch{return!1}})();function bt(t){return new Proxy(t,{get:function(e,n){return n in e?e[n]:e[N]}})}var So=m({},eo);So[N]=m(m(m(m({},{"fa-duotone":"duotone"}),eo[N]),wa.kit),wa["kit-duotone"]);var Ql=bt(So),fr=m({},Vu);fr[N]=m(m(m(m({},{duotone:"fad"}),fr[N]),Aa.kit),Aa["kit-duotone"]);var Ia=bt(fr),mr=m({},ur);mr[N]=m(m({},mr[N]),qu.kit);var Tr=bt(mr),hr=m({},$l);hr[N]=m(m({},hr[N]),Xu.kit);var tb=bt(hr),ed=yu,Mo="fa-layers-text",td=Du,nd=m({},zu),nb=bt(nd),rd=["class","data-prefix","data-icon","data-fa-transform","data-fa-mask"],rr=_u,id=[].concat(Z(Wu),Z(Kl)),mt=he.FontAwesomeConfig||{};function ad(t){var r=M.querySelector("script["+t+"]");if(r)return r.getAttribute(t)}function od(t){return t===""?!0:t==="false"?!1:t==="true"?!0:t}M&&typeof M.querySelector=="function"&&(Ta=[["data-family-prefix","familyPrefix"],["data-css-prefix","cssPrefix"],["data-family-default","familyDefault"],["data-style-default","styleDefault"],["data-replacement-class","replacementClass"],["data-auto-replace-svg","autoReplaceSvg"],["data-auto-add-css","autoAddCss"],["data-search-pseudo-elements","searchPseudoElements"],["data-search-pseudo-elements-warnings","searchPseudoElementsWarnings"],["data-search-pseudo-elements-full-scan","searchPseudoElementsFullScan"],["data-observe-mutations","observeMutations"],["data-mutate-approach","mutateApproach"],["data-keep-original-source","keepOriginalSource"],["data-measure-performance","measurePerformance"],["data-show-missing-icons","showMissingIcons"]],Ta.forEach(function(t){var r=on(t,2),e=r[0],n=r[1],i=od(ad(e));i!=null&&(mt[n]=i)}));var Ta,xo={styleDefault:"solid",familyDefault:N,cssPrefix:Ao,replacementClass:Co,autoReplaceSvg:!0,autoAddCss:!0,searchPseudoElements:!1,searchPseudoElementsWarnings:!0,searchPseudoElementsFullScan:!1,observeMutations:!0,mutateApproach:"async",keepOriginalSource:!0,measurePerformance:!1,showMissingIcons:!0};mt.familyPrefix&&(mt.cssPrefix=mt.familyPrefix);var $e=m(m({},xo),mt);$e.autoReplaceSvg||($e.observeMutations=!1);var b={};Object.keys(xo).forEach(function(t){Object.defineProperty(b,t,{enumerable:!0,set:function(e){$e[t]=e,ht.forEach(function(n){return n(b)})},get:function(){return $e[t]}})});Object.defineProperty(b,"familyPrefix",{enumerable:!0,set:function(r){$e.cssPrefix=r,ht.forEach(function(e){return e(b)})},get:function(){return $e.cssPrefix}});he.FontAwesomeConfig=b;var ht=[];function sd(t){return ht.push(t),function(){ht.splice(ht.indexOf(t),1)}}var me=lr,Q={size:16,x:0,y:0,rotate:0,flipX:!1,flipY:!1};function cd(t){if(!(!t||!se)){var r=M.createElement("style");r.setAttribute("type","text/css"),r.innerHTML=t;for(var e=M.head.childNodes,n=null,i=e.length-1;i>-1;i--){var a=e[i],o=(a.tagName||"").toUpperCase();["STYLE","LINK"].indexOf(o)>-1&&(n=a)}return M.head.insertBefore(r,n),t}}var ud="0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";function Sa(){for(var t=12,r="";t-- >0;)r+=ud[Math.random()*62|0];return r}function He(t){for(var r=[],e=(t||[]).length>>>0;e--;)r[e]=t[e];return r}function Sr(t){return t.classList?He(t.classList):(t.getAttribute("class")||"").split(" ").filter(function(r){return r})}function Fo(t){return"".concat(t).replace(/&/g,"&amp;").replace(/"/g,"&quot;").replace(/'/g,"&#39;").replace(/</g,"&lt;").replace(/>/g,"&gt;")}function ld(t){return Object.keys(t||{}).reduce(function(r,e){return r+"".concat(e,'="').concat(Fo(t[e]),'" ')},"").trim()}function sn(t){return Object.keys(t||{}).reduce(function(r,e){return r+"".concat(e,": ").concat(t[e].trim(),";")},"")}function Mr(t){return t.size!==Q.size||t.x!==Q.x||t.y!==Q.y||t.rotate!==Q.rotate||t.flipX||t.flipY}function dd(t){var r=t.transform,e=t.containerWidth,n=t.iconWidth,i={transform:"translate(".concat(e/2," 256)")},a="translate(".concat(r.x*32,", ").concat(r.y*32,") "),o="scale(".concat(r.size/16*(r.flipX?-1:1),", ").concat(r.size/16*(r.flipY?-1:1),") "),s="rotate(".concat(r.rotate," 0 0)"),c={transform:"".concat(a," ").concat(o," ").concat(s)},u={transform:"translate(".concat(n/2*-1," -256)")};return{outer:i,inner:c,path:u}}function fd(t){var r=t.transform,e=t.width,n=e===void 0?lr:e,i=t.height,a=i===void 0?lr:i,o=t.startCentered,s=o===void 0?!1:o,c="";return s&&Qa?c+="translate(".concat(r.x/me-n/2,"em, ").concat(r.y/me-a/2,"em) "):s?c+="translate(calc(-50% + ".concat(r.x/me,"em), calc(-50% + ").concat(r.y/me,"em)) "):c+="translate(".concat(r.x/me,"em, ").concat(r.y/me,"em) "),c+="scale(".concat(r.size/me*(r.flipX?-1:1),", ").concat(r.size/me*(r.flipY?-1:1),") "),c+="rotate(".concat(r.rotate,"deg) "),c}var md=`:root, :host {
  --fa-font-solid: normal 900 1em/1 'Font Awesome 7 Free';
  --fa-font-regular: normal 400 1em/1 'Font Awesome 7 Free';
  --fa-font-light: normal 300 1em/1 'Font Awesome 7 Pro';
  --fa-font-thin: normal 100 1em/1 'Font Awesome 7 Pro';
  --fa-font-duotone: normal 900 1em/1 'Font Awesome 7 Duotone';
  --fa-font-duotone-regular: normal 400 1em/1 'Font Awesome 7 Duotone';
  --fa-font-duotone-light: normal 300 1em/1 'Font Awesome 7 Duotone';
  --fa-font-duotone-thin: normal 100 1em/1 'Font Awesome 7 Duotone';
  --fa-font-brands: normal 400 1em/1 'Font Awesome 7 Brands';
  --fa-font-sharp-solid: normal 900 1em/1 'Font Awesome 7 Sharp';
  --fa-font-sharp-regular: normal 400 1em/1 'Font Awesome 7 Sharp';
  --fa-font-sharp-light: normal 300 1em/1 'Font Awesome 7 Sharp';
  --fa-font-sharp-thin: normal 100 1em/1 'Font Awesome 7 Sharp';
  --fa-font-sharp-duotone-solid: normal 900 1em/1 'Font Awesome 7 Sharp Duotone';
  --fa-font-sharp-duotone-regular: normal 400 1em/1 'Font Awesome 7 Sharp Duotone';
  --fa-font-sharp-duotone-light: normal 300 1em/1 'Font Awesome 7 Sharp Duotone';
  --fa-font-sharp-duotone-thin: normal 100 1em/1 'Font Awesome 7 Sharp Duotone';
  --fa-font-slab-regular: normal 400 1em/1 'Font Awesome 7 Slab';
  --fa-font-slab-press-regular: normal 400 1em/1 'Font Awesome 7 Slab Press';
  --fa-font-whiteboard-semibold: normal 600 1em/1 'Font Awesome 7 Whiteboard';
  --fa-font-thumbprint-light: normal 300 1em/1 'Font Awesome 7 Thumbprint';
  --fa-font-notdog-solid: normal 900 1em/1 'Font Awesome 7 Notdog';
  --fa-font-notdog-duo-solid: normal 900 1em/1 'Font Awesome 7 Notdog Duo';
  --fa-font-etch-solid: normal 900 1em/1 'Font Awesome 7 Etch';
  --fa-font-graphite-thin: normal 100 1em/1 'Font Awesome 7 Graphite';
  --fa-font-jelly-regular: normal 400 1em/1 'Font Awesome 7 Jelly';
  --fa-font-jelly-fill-regular: normal 400 1em/1 'Font Awesome 7 Jelly Fill';
  --fa-font-jelly-duo-regular: normal 400 1em/1 'Font Awesome 7 Jelly Duo';
  --fa-font-chisel-regular: normal 400 1em/1 'Font Awesome 7 Chisel';
  --fa-font-utility-semibold: normal 600 1em/1 'Font Awesome 7 Utility';
  --fa-font-utility-duo-semibold: normal 600 1em/1 'Font Awesome 7 Utility Duo';
  --fa-font-utility-fill-semibold: normal 600 1em/1 'Font Awesome 7 Utility Fill';
}

.svg-inline--fa {
  box-sizing: content-box;
  display: var(--fa-display, inline-block);
  height: 1em;
  overflow: visible;
  vertical-align: -0.125em;
  width: var(--fa-width, 1.25em);
}
.svg-inline--fa.fa-2xs {
  vertical-align: 0.1em;
}
.svg-inline--fa.fa-xs {
  vertical-align: 0em;
}
.svg-inline--fa.fa-sm {
  vertical-align: -0.0714285714em;
}
.svg-inline--fa.fa-lg {
  vertical-align: -0.2em;
}
.svg-inline--fa.fa-xl {
  vertical-align: -0.25em;
}
.svg-inline--fa.fa-2xl {
  vertical-align: -0.3125em;
}
.svg-inline--fa.fa-pull-left,
.svg-inline--fa .fa-pull-start {
  float: inline-start;
  margin-inline-end: var(--fa-pull-margin, 0.3em);
}
.svg-inline--fa.fa-pull-right,
.svg-inline--fa .fa-pull-end {
  float: inline-end;
  margin-inline-start: var(--fa-pull-margin, 0.3em);
}
.svg-inline--fa.fa-li {
  width: var(--fa-li-width, 2em);
  inset-inline-start: calc(-1 * var(--fa-li-width, 2em));
  inset-block-start: 0.25em; /* syncing vertical alignment with Web Font rendering */
}

.fa-layers-counter, .fa-layers-text {
  display: inline-block;
  position: absolute;
  text-align: center;
}

.fa-layers {
  display: inline-block;
  height: 1em;
  position: relative;
  text-align: center;
  vertical-align: -0.125em;
  width: var(--fa-width, 1.25em);
}
.fa-layers .svg-inline--fa {
  inset: 0;
  margin: auto;
  position: absolute;
  transform-origin: center center;
}

.fa-layers-text {
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  transform-origin: center center;
}

.fa-layers-counter {
  background-color: var(--fa-counter-background-color, #ff253a);
  border-radius: var(--fa-counter-border-radius, 1em);
  box-sizing: border-box;
  color: var(--fa-inverse, #fff);
  line-height: var(--fa-counter-line-height, 1);
  max-width: var(--fa-counter-max-width, 5em);
  min-width: var(--fa-counter-min-width, 1.5em);
  overflow: hidden;
  padding: var(--fa-counter-padding, 0.25em 0.5em);
  right: var(--fa-right, 0);
  text-overflow: ellipsis;
  top: var(--fa-top, 0);
  transform: scale(var(--fa-counter-scale, 0.25));
  transform-origin: top right;
}

.fa-layers-bottom-right {
  bottom: var(--fa-bottom, 0);
  right: var(--fa-right, 0);
  top: auto;
  transform: scale(var(--fa-layers-scale, 0.25));
  transform-origin: bottom right;
}

.fa-layers-bottom-left {
  bottom: var(--fa-bottom, 0);
  left: var(--fa-left, 0);
  right: auto;
  top: auto;
  transform: scale(var(--fa-layers-scale, 0.25));
  transform-origin: bottom left;
}

.fa-layers-top-right {
  top: var(--fa-top, 0);
  right: var(--fa-right, 0);
  transform: scale(var(--fa-layers-scale, 0.25));
  transform-origin: top right;
}

.fa-layers-top-left {
  left: var(--fa-left, 0);
  right: auto;
  top: var(--fa-top, 0);
  transform: scale(var(--fa-layers-scale, 0.25));
  transform-origin: top left;
}

.fa-1x {
  font-size: 1em;
}

.fa-2x {
  font-size: 2em;
}

.fa-3x {
  font-size: 3em;
}

.fa-4x {
  font-size: 4em;
}

.fa-5x {
  font-size: 5em;
}

.fa-6x {
  font-size: 6em;
}

.fa-7x {
  font-size: 7em;
}

.fa-8x {
  font-size: 8em;
}

.fa-9x {
  font-size: 9em;
}

.fa-10x {
  font-size: 10em;
}

.fa-2xs {
  font-size: calc(10 / 16 * 1em); /* converts a 10px size into an em-based value that's relative to the scale's 16px base */
  line-height: calc(1 / 10 * 1em); /* sets the line-height of the icon back to that of it's parent */
  vertical-align: calc((6 / 10 - 0.375) * 1em); /* vertically centers the icon taking into account the surrounding text's descender */
}

.fa-xs {
  font-size: calc(12 / 16 * 1em); /* converts a 12px size into an em-based value that's relative to the scale's 16px base */
  line-height: calc(1 / 12 * 1em); /* sets the line-height of the icon back to that of it's parent */
  vertical-align: calc((6 / 12 - 0.375) * 1em); /* vertically centers the icon taking into account the surrounding text's descender */
}

.fa-sm {
  font-size: calc(14 / 16 * 1em); /* converts a 14px size into an em-based value that's relative to the scale's 16px base */
  line-height: calc(1 / 14 * 1em); /* sets the line-height of the icon back to that of it's parent */
  vertical-align: calc((6 / 14 - 0.375) * 1em); /* vertically centers the icon taking into account the surrounding text's descender */
}

.fa-lg {
  font-size: calc(20 / 16 * 1em); /* converts a 20px size into an em-based value that's relative to the scale's 16px base */
  line-height: calc(1 / 20 * 1em); /* sets the line-height of the icon back to that of it's parent */
  vertical-align: calc((6 / 20 - 0.375) * 1em); /* vertically centers the icon taking into account the surrounding text's descender */
}

.fa-xl {
  font-size: calc(24 / 16 * 1em); /* converts a 24px size into an em-based value that's relative to the scale's 16px base */
  line-height: calc(1 / 24 * 1em); /* sets the line-height of the icon back to that of it's parent */
  vertical-align: calc((6 / 24 - 0.375) * 1em); /* vertically centers the icon taking into account the surrounding text's descender */
}

.fa-2xl {
  font-size: calc(32 / 16 * 1em); /* converts a 32px size into an em-based value that's relative to the scale's 16px base */
  line-height: calc(1 / 32 * 1em); /* sets the line-height of the icon back to that of it's parent */
  vertical-align: calc((6 / 32 - 0.375) * 1em); /* vertically centers the icon taking into account the surrounding text's descender */
}

.fa-width-auto {
  --fa-width: auto;
}

.fa-fw,
.fa-width-fixed {
  --fa-width: 1.25em;
}

.fa-ul {
  list-style-type: none;
  margin-inline-start: var(--fa-li-margin, 2.5em);
  padding-inline-start: 0;
}
.fa-ul > li {
  position: relative;
}

.fa-li {
  inset-inline-start: calc(-1 * var(--fa-li-width, 2em));
  position: absolute;
  text-align: center;
  width: var(--fa-li-width, 2em);
  line-height: inherit;
}

/* Heads Up: Bordered Icons will not be supported in the future!
  - This feature will be deprecated in the next major release of Font Awesome (v8)!
  - You may continue to use it in this version *v7), but it will not be supported in Font Awesome v8.
*/
/* Notes:
* --@{v.$css-prefix}-border-width = 1/16 by default (to render as ~1px based on a 16px default font-size)
* --@{v.$css-prefix}-border-padding =
  ** 3/16 for vertical padding (to give ~2px of vertical whitespace around an icon considering it's vertical alignment)
  ** 4/16 for horizontal padding (to give ~4px of horizontal whitespace around an icon)
*/
.fa-border {
  border-color: var(--fa-border-color, #eee);
  border-radius: var(--fa-border-radius, 0.1em);
  border-style: var(--fa-border-style, solid);
  border-width: var(--fa-border-width, 0.0625em);
  box-sizing: var(--fa-border-box-sizing, content-box);
  padding: var(--fa-border-padding, 0.1875em 0.25em);
}

.fa-pull-left,
.fa-pull-start {
  float: inline-start;
  margin-inline-end: var(--fa-pull-margin, 0.3em);
}

.fa-pull-right,
.fa-pull-end {
  float: inline-end;
  margin-inline-start: var(--fa-pull-margin, 0.3em);
}

.fa-beat {
  animation-name: fa-beat;
  animation-delay: var(--fa-animation-delay, 0s);
  animation-direction: var(--fa-animation-direction, normal);
  animation-duration: var(--fa-animation-duration, 1s);
  animation-iteration-count: var(--fa-animation-iteration-count, infinite);
  animation-timing-function: var(--fa-animation-timing, ease-in-out);
}

.fa-bounce {
  animation-name: fa-bounce;
  animation-delay: var(--fa-animation-delay, 0s);
  animation-direction: var(--fa-animation-direction, normal);
  animation-duration: var(--fa-animation-duration, 1s);
  animation-iteration-count: var(--fa-animation-iteration-count, infinite);
  animation-timing-function: var(--fa-animation-timing, cubic-bezier(0.28, 0.84, 0.42, 1));
}

.fa-fade {
  animation-name: fa-fade;
  animation-delay: var(--fa-animation-delay, 0s);
  animation-direction: var(--fa-animation-direction, normal);
  animation-duration: var(--fa-animation-duration, 1s);
  animation-iteration-count: var(--fa-animation-iteration-count, infinite);
  animation-timing-function: var(--fa-animation-timing, cubic-bezier(0.4, 0, 0.6, 1));
}

.fa-beat-fade {
  animation-name: fa-beat-fade;
  animation-delay: var(--fa-animation-delay, 0s);
  animation-direction: var(--fa-animation-direction, normal);
  animation-duration: var(--fa-animation-duration, 1s);
  animation-iteration-count: var(--fa-animation-iteration-count, infinite);
  animation-timing-function: var(--fa-animation-timing, cubic-bezier(0.4, 0, 0.6, 1));
}

.fa-flip {
  animation-name: fa-flip;
  animation-delay: var(--fa-animation-delay, 0s);
  animation-direction: var(--fa-animation-direction, normal);
  animation-duration: var(--fa-animation-duration, 1s);
  animation-iteration-count: var(--fa-animation-iteration-count, infinite);
  animation-timing-function: var(--fa-animation-timing, ease-in-out);
}

.fa-shake {
  animation-name: fa-shake;
  animation-delay: var(--fa-animation-delay, 0s);
  animation-direction: var(--fa-animation-direction, normal);
  animation-duration: var(--fa-animation-duration, 1s);
  animation-iteration-count: var(--fa-animation-iteration-count, infinite);
  animation-timing-function: var(--fa-animation-timing, linear);
}

.fa-spin {
  animation-name: fa-spin;
  animation-delay: var(--fa-animation-delay, 0s);
  animation-direction: var(--fa-animation-direction, normal);
  animation-duration: var(--fa-animation-duration, 2s);
  animation-iteration-count: var(--fa-animation-iteration-count, infinite);
  animation-timing-function: var(--fa-animation-timing, linear);
}

.fa-spin-reverse {
  --fa-animation-direction: reverse;
}

.fa-pulse,
.fa-spin-pulse {
  animation-name: fa-spin;
  animation-direction: var(--fa-animation-direction, normal);
  animation-duration: var(--fa-animation-duration, 1s);
  animation-iteration-count: var(--fa-animation-iteration-count, infinite);
  animation-timing-function: var(--fa-animation-timing, steps(8));
}

@media (prefers-reduced-motion: reduce) {
  .fa-beat,
  .fa-bounce,
  .fa-fade,
  .fa-beat-fade,
  .fa-flip,
  .fa-pulse,
  .fa-shake,
  .fa-spin,
  .fa-spin-pulse {
    animation: none !important;
    transition: none !important;
  }
}
@keyframes fa-beat {
  0%, 90% {
    transform: scale(1);
  }
  45% {
    transform: scale(var(--fa-beat-scale, 1.25));
  }
}
@keyframes fa-bounce {
  0% {
    transform: scale(1, 1) translateY(0);
  }
  10% {
    transform: scale(var(--fa-bounce-start-scale-x, 1.1), var(--fa-bounce-start-scale-y, 0.9)) translateY(0);
  }
  30% {
    transform: scale(var(--fa-bounce-jump-scale-x, 0.9), var(--fa-bounce-jump-scale-y, 1.1)) translateY(var(--fa-bounce-height, -0.5em));
  }
  50% {
    transform: scale(var(--fa-bounce-land-scale-x, 1.05), var(--fa-bounce-land-scale-y, 0.95)) translateY(0);
  }
  57% {
    transform: scale(1, 1) translateY(var(--fa-bounce-rebound, -0.125em));
  }
  64% {
    transform: scale(1, 1) translateY(0);
  }
  100% {
    transform: scale(1, 1) translateY(0);
  }
}
@keyframes fa-fade {
  50% {
    opacity: var(--fa-fade-opacity, 0.4);
  }
}
@keyframes fa-beat-fade {
  0%, 100% {
    opacity: var(--fa-beat-fade-opacity, 0.4);
    transform: scale(1);
  }
  50% {
    opacity: 1;
    transform: scale(var(--fa-beat-fade-scale, 1.125));
  }
}
@keyframes fa-flip {
  50% {
    transform: rotate3d(var(--fa-flip-x, 0), var(--fa-flip-y, 1), var(--fa-flip-z, 0), var(--fa-flip-angle, -180deg));
  }
}
@keyframes fa-shake {
  0% {
    transform: rotate(-15deg);
  }
  4% {
    transform: rotate(15deg);
  }
  8%, 24% {
    transform: rotate(-18deg);
  }
  12%, 28% {
    transform: rotate(18deg);
  }
  16% {
    transform: rotate(-22deg);
  }
  20% {
    transform: rotate(22deg);
  }
  32% {
    transform: rotate(-12deg);
  }
  36% {
    transform: rotate(12deg);
  }
  40%, 100% {
    transform: rotate(0deg);
  }
}
@keyframes fa-spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}
.fa-rotate-90 {
  transform: rotate(90deg);
}

.fa-rotate-180 {
  transform: rotate(180deg);
}

.fa-rotate-270 {
  transform: rotate(270deg);
}

.fa-flip-horizontal {
  transform: scale(-1, 1);
}

.fa-flip-vertical {
  transform: scale(1, -1);
}

.fa-flip-both,
.fa-flip-horizontal.fa-flip-vertical {
  transform: scale(-1, -1);
}

.fa-rotate-by {
  transform: rotate(var(--fa-rotate-angle, 0));
}

.svg-inline--fa .fa-primary {
  fill: var(--fa-primary-color, currentColor);
  opacity: var(--fa-primary-opacity, 1);
}

.svg-inline--fa .fa-secondary {
  fill: var(--fa-secondary-color, currentColor);
  opacity: var(--fa-secondary-opacity, 0.4);
}

.svg-inline--fa.fa-swap-opacity .fa-primary {
  opacity: var(--fa-secondary-opacity, 0.4);
}

.svg-inline--fa.fa-swap-opacity .fa-secondary {
  opacity: var(--fa-primary-opacity, 1);
}

.svg-inline--fa mask .fa-primary,
.svg-inline--fa mask .fa-secondary {
  fill: black;
}

.svg-inline--fa.fa-inverse {
  fill: var(--fa-inverse, #fff);
}

.fa-stack {
  display: inline-block;
  height: 2em;
  line-height: 2em;
  position: relative;
  vertical-align: middle;
  width: 2.5em;
}

.fa-inverse {
  color: var(--fa-inverse, #fff);
}

.svg-inline--fa.fa-stack-1x {
  --fa-width: 1.25em;
  height: 1em;
  width: var(--fa-width);
}
.svg-inline--fa.fa-stack-2x {
  --fa-width: 2.5em;
  height: 2em;
  width: var(--fa-width);
}

.fa-stack-1x,
.fa-stack-2x {
  inset: 0;
  margin: auto;
  position: absolute;
  z-index: var(--fa-stack-z-index, auto);
}`;function Ro(){var t=Ao,r=Co,e=b.cssPrefix,n=b.replacementClass,i=md;if(e!==t||n!==r){var a=new RegExp("\\.".concat(t,"\\-"),"g"),o=new RegExp("\\--".concat(t,"\\-"),"g"),s=new RegExp("\\.".concat(r),"g");i=i.replace(a,".".concat(e,"-")).replace(o,"--".concat(e,"-")).replace(s,".".concat(n))}return i}var Ma=!1;function ir(){b.autoAddCss&&!Ma&&(cd(Ro()),Ma=!0)}var hd={mixout:function(){return{dom:{css:Ro,insertCss:ir}}},hooks:function(){return{beforeDOMElementCreation:function(){ir()},beforeI2svg:function(){ir()}}}},oe=he||{};oe[ae]||(oe[ae]={});oe[ae].styles||(oe[ae].styles={});oe[ae].hooks||(oe[ae].hooks={});oe[ae].shims||(oe[ae].shims=[]);var K=oe[ae],Oo=[],ko=function(){M.removeEventListener("DOMContentLoaded",ko),rn=1,Oo.map(function(r){return r()})},rn=!1;se&&(rn=(M.documentElement.doScroll?/^loaded|^c/:/^loaded|^i|^c/).test(M.readyState),rn||M.addEventListener("DOMContentLoaded",ko));function pd(t){se&&(rn?setTimeout(t,0):Oo.push(t))}function vt(t){var r=t.tag,e=t.attributes,n=e===void 0?{}:e,i=t.children,a=i===void 0?[]:i;return typeof t=="string"?Fo(t):"<".concat(r," ").concat(ld(n),">").concat(a.map(vt).join(""),"</").concat(r,">")}function xa(t,r,e){if(t&&t[r]&&t[r][e])return{prefix:r,iconName:e,icon:t[r][e]}}var gd=function(r,e){return function(n,i,a,o){return r.call(e,n,i,a,o)}},ar=function(r,e,n,i){var a=Object.keys(r),o=a.length,s=i!==void 0?gd(e,i):e,c,u,f;for(n===void 0?(c=1,f=r[a[0]]):(c=0,f=n);c<o;c++)u=a[c],f=s(f,r[u],u,r);return f};function No(t){return Z(t).length!==1?null:t.codePointAt(0).toString(16)}function Fa(t){return Object.keys(t).reduce(function(r,e){var n=t[e],i=!!n.icon;return i?r[n.iconName]=n.icon:r[e]=n,r},{})}function pr(t,r){var e=arguments.length>2&&arguments[2]!==void 0?arguments[2]:{},n=e.skipHooks,i=n===void 0?!1:n,a=Fa(r);typeof K.hooks.addPack=="function"&&!i?K.hooks.addPack(t,Fa(r)):K.styles[t]=m(m({},K.styles[t]||{}),a),t==="fas"&&pr("fa",r)}var pt=K.styles,bd=K.shims,Po=Object.keys(Tr),vd=Po.reduce(function(t,r){return t[r]=Object.keys(Tr[r]),t},{}),xr=null,Lo={},Bo={},jo={},Uo={},zo={};function yd(t){return~id.indexOf(t)}function Dd(t,r){var e=r.split("-"),n=e[0],i=e.slice(1).join("-");return n===t&&i!==""&&!yd(i)?i:null}var $o=function(){var r=function(a){return ar(pt,function(o,s,c){return o[c]=ar(s,a,{}),o},{})};Lo=r(function(i,a,o){if(a[3]&&(i[a[3]]=o),a[2]){var s=a[2].filter(function(c){return typeof c=="number"});s.forEach(function(c){i[c.toString(16)]=o})}return i}),Bo=r(function(i,a,o){if(i[o]=o,a[2]){var s=a[2].filter(function(c){return typeof c=="string"});s.forEach(function(c){i[c]=o})}return i}),zo=r(function(i,a,o){var s=a[2];return i[o]=o,s.forEach(function(c){i[c]=o}),i});var e="far"in pt||b.autoFetchSvg,n=ar(bd,function(i,a){var o=a[0],s=a[1],c=a[2];return s==="far"&&!e&&(s="fas"),typeof o=="string"&&(i.names[o]={prefix:s,iconName:c}),typeof o=="number"&&(i.unicodes[o.toString(16)]={prefix:s,iconName:c}),i},{names:{},unicodes:{}});jo=n.names,Uo=n.unicodes,xr=cn(b.styleDefault,{family:b.familyDefault})};sd(function(t){xr=cn(t.styleDefault,{family:b.familyDefault})});$o();function Fr(t,r){return(Lo[t]||{})[r]}function _d(t,r){return(Bo[t]||{})[r]}function Ee(t,r){return(zo[t]||{})[r]}function Ho(t){return jo[t]||{prefix:null,iconName:null}}function Ed(t){var r=Uo[t],e=Fr("fas",t);return r||(e?{prefix:"fas",iconName:e}:null)||{prefix:null,iconName:null}}function pe(){return xr}var Vo=function(){return{prefix:null,iconName:null,rest:[]}};function wd(t){var r=N,e=Po.reduce(function(n,i){return n[i]="".concat(b.cssPrefix,"-").concat(i),n},{});return Do.forEach(function(n){(t.includes(e[n])||t.some(function(i){return vd[n].includes(i)}))&&(r=n)}),r}function cn(t){var r=arguments.length>1&&arguments[1]!==void 0?arguments[1]:{},e=r.family,n=e===void 0?N:e,i=Ql[n][t];if(n===gt&&!t)return"fad";var a=Ia[n][t]||Ia[n][i],o=t in K.styles?t:null,s=a||o||null;return s}function Ad(t){var r=[],e=null;return t.forEach(function(n){var i=Dd(b.cssPrefix,n);i?e=i:n&&r.push(n)}),{iconName:e,rest:r}}function Ra(t){return t.sort().filter(function(r,e,n){return n.indexOf(r)===e})}var Oa=Eo.concat(_o);function un(t){var r=arguments.length>1&&arguments[1]!==void 0?arguments[1]:{},e=r.skipLookups,n=e===void 0?!1:e,i=null,a=Ra(t.filter(function(h){return Oa.includes(h)})),o=Ra(t.filter(function(h){return!Oa.includes(h)})),s=a.filter(function(h){return i=h,!to.includes(h)}),c=on(s,1),u=c[0],f=u===void 0?null:u,d=wd(a),p=m(m({},Ad(o)),{},{prefix:cn(f,{family:d})});return m(m(m({},p),Sd({values:t,family:d,styles:pt,config:b,canonical:p,givenPrefix:i})),Cd(n,i,p))}function Cd(t,r,e){var n=e.prefix,i=e.iconName;if(t||!n||!i)return{prefix:n,iconName:i};var a=r==="fa"?Ho(i):{},o=Ee(n,i);return i=a.iconName||o||i,n=a.prefix||n,n==="far"&&!pt.far&&pt.fas&&!b.autoFetchSvg&&(n="fas"),{prefix:n,iconName:i}}var Id=Do.filter(function(t){return t!==N||t!==gt}),Td=Object.keys(ur).filter(function(t){return t!==N}).map(function(t){return Object.keys(ur[t])}).flat();function Sd(t){var r=t.values,e=t.family,n=t.canonical,i=t.givenPrefix,a=i===void 0?"":i,o=t.styles,s=o===void 0?{}:o,c=t.config,u=c===void 0?{}:c,f=e===gt,d=r.includes("fa-duotone")||r.includes("fad"),p=u.familyDefault==="duotone",h=n.prefix==="fad"||n.prefix==="fa-duotone";if(!f&&(d||p||h)&&(n.prefix="fad"),(r.includes("fa-brands")||r.includes("fab"))&&(n.prefix="fab"),!n.prefix&&Id.includes(e)){var E=Object.keys(s).find(function(v){return Td.includes(v)});if(E||u.autoFetchSvg){var _=Hu.get(e).defaultShortPrefixId;n.prefix=_,n.iconName=Ee(n.prefix,n.iconName)||n.iconName}}return(n.prefix==="fa"||a==="fa")&&(n.prefix=pe()||"fas"),n}var Md=(function(){function t(){du(this,t),this.definitions={}}return fu(t,[{key:"add",value:function(){for(var e=this,n=arguments.length,i=new Array(n),a=0;a<n;a++)i[a]=arguments[a];var o=i.reduce(this._pullDefinitions,{});Object.keys(o).forEach(function(s){e.definitions[s]=m(m({},e.definitions[s]||{}),o[s]),pr(s,o[s]);var c=Tr[N][s];c&&pr(c,o[s]),$o()})}},{key:"reset",value:function(){this.definitions={}}},{key:"_pullDefinitions",value:function(e,n){var i=n.prefix&&n.iconName&&n.icon?{0:n}:n;return Object.keys(i).map(function(a){var o=i[a],s=o.prefix,c=o.iconName,u=o.icon,f=u[2];e[s]||(e[s]={}),f.length>0&&f.forEach(function(d){typeof d=="string"&&(e[s][d]=u)}),e[s][c]=u}),e}}])})(),ka=[],Ue={},ze={},xd=Object.keys(ze);function Fd(t,r){var e=r.mixoutsTo;return ka=t,Ue={},Object.keys(ze).forEach(function(n){xd.indexOf(n)===-1&&delete ze[n]}),ka.forEach(function(n){var i=n.mixout?n.mixout():{};if(Object.keys(i).forEach(function(o){typeof i[o]=="function"&&(e[o]=i[o]),nn(i[o])==="object"&&Object.keys(i[o]).forEach(function(s){e[o]||(e[o]={}),e[o][s]=i[o][s]})}),n.hooks){var a=n.hooks();Object.keys(a).forEach(function(o){Ue[o]||(Ue[o]=[]),Ue[o].push(a[o])})}n.provides&&n.provides(ze)}),e}function gr(t,r){for(var e=arguments.length,n=new Array(e>2?e-2:0),i=2;i<e;i++)n[i-2]=arguments[i];var a=Ue[t]||[];return a.forEach(function(o){r=o.apply(null,[r].concat(n))}),r}function Ae(t){for(var r=arguments.length,e=new Array(r>1?r-1:0),n=1;n<r;n++)e[n-1]=arguments[n];var i=Ue[t]||[];i.forEach(function(a){a.apply(null,e)})}function ge(){var t=arguments[0],r=Array.prototype.slice.call(arguments,1);return ze[t]?ze[t].apply(null,r):void 0}function br(t){t.prefix==="fa"&&(t.prefix="fas");var r=t.iconName,e=t.prefix||pe();if(r)return r=Ee(e,r)||r,xa(Wo.definitions,e,r)||xa(K.styles,e,r)}var Wo=new Md,Rd=function(){b.autoReplaceSvg=!1,b.observeMutations=!1,Ae("noAuto")},Od={i2svg:function(){var r=arguments.length>0&&arguments[0]!==void 0?arguments[0]:{};return se?(Ae("beforeI2svg",r),ge("pseudoElements2svg",r),ge("i2svg",r)):Promise.reject(new Error("Operation requires a DOM of some kind."))},watch:function(){var r=arguments.length>0&&arguments[0]!==void 0?arguments[0]:{},e=r.autoReplaceSvgRoot;b.autoReplaceSvg===!1&&(b.autoReplaceSvg=!0),b.observeMutations=!0,pd(function(){Nd({autoReplaceSvgRoot:e}),Ae("watch",r)})}},kd={icon:function(r){if(r===null)return null;if(nn(r)==="object"&&r.prefix&&r.iconName)return{prefix:r.prefix,iconName:Ee(r.prefix,r.iconName)||r.iconName};if(Array.isArray(r)&&r.length===2){var e=r[1].indexOf("fa-")===0?r[1].slice(3):r[1],n=cn(r[0]);return{prefix:n,iconName:Ee(n,e)||e}}if(typeof r=="string"&&(r.indexOf("".concat(b.cssPrefix,"-"))>-1||r.match(ed))){var i=un(r.split(" "),{skipLookups:!0});return{prefix:i.prefix||pe(),iconName:Ee(i.prefix,i.iconName)||i.iconName}}if(typeof r=="string"){var a=pe();return{prefix:a,iconName:Ee(a,r)||r}}}},G={noAuto:Rd,config:b,dom:Od,parse:kd,library:Wo,findIconDefinition:br,toHtml:vt},Nd=function(){var r=arguments.length>0&&arguments[0]!==void 0?arguments[0]:{},e=r.autoReplaceSvgRoot,n=e===void 0?M:e;(Object.keys(K.styles).length>0||b.autoFetchSvg)&&se&&b.autoReplaceSvg&&G.dom.i2svg({node:n})};function ln(t,r){return Object.defineProperty(t,"abstract",{get:r}),Object.defineProperty(t,"html",{get:function(){return t.abstract.map(function(n){return vt(n)})}}),Object.defineProperty(t,"node",{get:function(){if(se){var n=M.createElement("div");return n.innerHTML=t.html,n.children}}}),t}function Pd(t){var r=t.children,e=t.main,n=t.mask,i=t.attributes,a=t.styles,o=t.transform;if(Mr(o)&&e.found&&!n.found){var s=e.width,c=e.height,u={x:s/c/2,y:.5};i.style=sn(m(m({},a),{},{"transform-origin":"".concat(u.x+o.x/16,"em ").concat(u.y+o.y/16,"em")}))}return[{tag:"svg",attributes:i,children:r}]}function Ld(t){var r=t.prefix,e=t.iconName,n=t.children,i=t.attributes,a=t.symbol,o=a===!0?"".concat(r,"-").concat(b.cssPrefix,"-").concat(e):a;return[{tag:"svg",attributes:{style:"display: none;"},children:[{tag:"symbol",attributes:m(m({},i),{},{id:o}),children:n}]}]}function Bd(t){var r=["aria-label","aria-labelledby","title","role"];return r.some(function(e){return e in t})}function Rr(t){var r=t.icons,e=r.main,n=r.mask,i=t.prefix,a=t.iconName,o=t.transform,s=t.symbol,c=t.maskId,u=t.extra,f=t.watchable,d=f===void 0?!1:f,p=n.found?n:e,h=p.width,E=p.height,_=[b.replacementClass,a?"".concat(b.cssPrefix,"-").concat(a):""].filter(function(k){return u.classes.indexOf(k)===-1}).filter(function(k){return k!==""||!!k}).concat(u.classes).join(" "),v={children:[],attributes:m(m({},u.attributes),{},{"data-prefix":i,"data-icon":a,class:_,role:u.attributes.role||"img",viewBox:"0 0 ".concat(h," ").concat(E)})};!Bd(u.attributes)&&!u.attributes["aria-hidden"]&&(v.attributes["aria-hidden"]="true"),d&&(v.attributes[we]="");var y=m(m({},v),{},{prefix:i,iconName:a,main:e,mask:n,maskId:c,transform:o,symbol:s,styles:m({},u.styles)}),A=n.found&&e.found?ge("generateAbstractMask",y)||{children:[],attributes:{}}:ge("generateAbstractIcon",y)||{children:[],attributes:{}},x=A.children,T=A.attributes;return y.children=x,y.attributes=T,s?Ld(y):Pd(y)}function Na(t){var r=t.content,e=t.width,n=t.height,i=t.transform,a=t.extra,o=t.watchable,s=o===void 0?!1:o,c=m(m({},a.attributes),{},{class:a.classes.join(" ")});s&&(c[we]="");var u=m({},a.styles);Mr(i)&&(u.transform=fd({transform:i,startCentered:!0,width:e,height:n}),u["-webkit-transform"]=u.transform);var f=sn(u);f.length>0&&(c.style=f);var d=[];return d.push({tag:"span",attributes:c,children:[r]}),d}function jd(t){var r=t.content,e=t.extra,n=m(m({},e.attributes),{},{class:e.classes.join(" ")}),i=sn(e.styles);i.length>0&&(n.style=i);var a=[];return a.push({tag:"span",attributes:n,children:[r]}),a}var or=K.styles;function vr(t){var r=t[0],e=t[1],n=t.slice(4),i=on(n,1),a=i[0],o=null;return Array.isArray(a)?o={tag:"g",attributes:{class:"".concat(b.cssPrefix,"-").concat(rr.GROUP)},children:[{tag:"path",attributes:{class:"".concat(b.cssPrefix,"-").concat(rr.SECONDARY),fill:"currentColor",d:a[0]}},{tag:"path",attributes:{class:"".concat(b.cssPrefix,"-").concat(rr.PRIMARY),fill:"currentColor",d:a[1]}}]}:o={tag:"path",attributes:{fill:"currentColor",d:a}},{found:!0,width:r,height:e,icon:o}}var Ud={found:!1,width:512,height:512};function zd(t,r){!To&&!b.showMissingIcons&&t&&console.error('Icon with name "'.concat(t,'" and prefix "').concat(r,'" is missing.'))}function yr(t,r){var e=r;return r==="fa"&&b.styleDefault!==null&&(r=pe()),new Promise(function(n,i){if(e==="fa"){var a=Ho(t)||{};t=a.iconName||t,r=a.prefix||r}if(t&&r&&or[r]&&or[r][t]){var o=or[r][t];return n(vr(o))}zd(t,r),n(m(m({},Ud),{},{icon:b.showMissingIcons&&t?ge("missingIconAbstract")||{}:{}}))})}var Pa=function(){},Dr=b.measurePerformance&&Jt&&Jt.mark&&Jt.measure?Jt:{mark:Pa,measure:Pa},ft='FA "7.2.0"',$d=function(r){return Dr.mark("".concat(ft," ").concat(r," begins")),function(){return Go(r)}},Go=function(r){Dr.mark("".concat(ft," ").concat(r," ends")),Dr.measure("".concat(ft," ").concat(r),"".concat(ft," ").concat(r," begins"),"".concat(ft," ").concat(r," ends"))},Or={begin:$d,end:Go},en=function(){};function La(t){var r=t.getAttribute?t.getAttribute(we):null;return typeof r=="string"}function Hd(t){var r=t.getAttribute?t.getAttribute(Cr):null,e=t.getAttribute?t.getAttribute(Ir):null;return r&&e}function Vd(t){return t&&t.classList&&t.classList.contains&&t.classList.contains(b.replacementClass)}function Wd(){if(b.autoReplaceSvg===!0)return tn.replace;var t=tn[b.autoReplaceSvg];return t||tn.replace}function Gd(t){return M.createElementNS("http://www.w3.org/2000/svg",t)}function Yd(t){return M.createElement(t)}function Yo(t){var r=arguments.length>1&&arguments[1]!==void 0?arguments[1]:{},e=r.ceFn,n=e===void 0?t.tag==="svg"?Gd:Yd:e;if(typeof t=="string")return M.createTextNode(t);var i=n(t.tag);Object.keys(t.attributes||[]).forEach(function(o){i.setAttribute(o,t.attributes[o])});var a=t.children||[];return a.forEach(function(o){i.appendChild(Yo(o,{ceFn:n}))}),i}function Kd(t){var r=" ".concat(t.outerHTML," ");return r="".concat(r,"Font Awesome fontawesome.com "),r}var tn={replace:function(r){var e=r[0];if(e.parentNode)if(r[1].forEach(function(i){e.parentNode.insertBefore(Yo(i),e)}),e.getAttribute(we)===null&&b.keepOriginalSource){var n=M.createComment(Kd(e));e.parentNode.replaceChild(n,e)}else e.remove()},nest:function(r){var e=r[0],n=r[1];if(~Sr(e).indexOf(b.replacementClass))return tn.replace(r);var i=new RegExp("".concat(b.cssPrefix,"-.*"));if(delete n[0].attributes.id,n[0].attributes.class){var a=n[0].attributes.class.split(" ").reduce(function(s,c){return c===b.replacementClass||c.match(i)?s.toSvg.push(c):s.toNode.push(c),s},{toNode:[],toSvg:[]});n[0].attributes.class=a.toSvg.join(" "),a.toNode.length===0?e.removeAttribute("class"):e.setAttribute("class",a.toNode.join(" "))}var o=n.map(function(s){return vt(s)}).join(`
`);e.setAttribute(we,""),e.innerHTML=o}};function Ba(t){t()}function Ko(t,r){var e=typeof r=="function"?r:en;if(t.length===0)e();else{var n=Ba;b.mutateApproach===Jl&&(n=he.requestAnimationFrame||Ba),n(function(){var i=Wd(),a=Or.begin("mutate");t.map(i),a(),e()})}}var kr=!1;function Zo(){kr=!0}function _r(){kr=!1}var an=null;function ja(t){if(Ea&&b.observeMutations){var r=t.treeCallback,e=r===void 0?en:r,n=t.nodeCallback,i=n===void 0?en:n,a=t.pseudoElementsCallback,o=a===void 0?en:a,s=t.observeMutationsRoot,c=s===void 0?M:s;an=new Ea(function(u){if(!kr){var f=pe();He(u).forEach(function(d){if(d.type==="childList"&&d.addedNodes.length>0&&!La(d.addedNodes[0])&&(b.searchPseudoElements&&o(d.target),e(d.target)),d.type==="attributes"&&d.target.parentNode&&b.searchPseudoElements&&o([d.target],!0),d.type==="attributes"&&La(d.target)&&~rd.indexOf(d.attributeName))if(d.attributeName==="class"&&Hd(d.target)){var p=un(Sr(d.target)),h=p.prefix,E=p.iconName;d.target.setAttribute(Cr,h||f),E&&d.target.setAttribute(Ir,E)}else Vd(d.target)&&i(d.target)})}}),se&&an.observe(c,{childList:!0,attributes:!0,characterData:!0,subtree:!0})}}function Zd(){an&&an.disconnect()}function Xd(t){var r=t.getAttribute("style"),e=[];return r&&(e=r.split(";").reduce(function(n,i){var a=i.split(":"),o=a[0],s=a.slice(1);return o&&s.length>0&&(n[o]=s.join(":").trim()),n},{})),e}function Jd(t){var r=t.getAttribute("data-prefix"),e=t.getAttribute("data-icon"),n=t.innerText!==void 0?t.innerText.trim():"",i=un(Sr(t));return i.prefix||(i.prefix=pe()),r&&e&&(i.prefix=r,i.iconName=e),i.iconName&&i.prefix||(i.prefix&&n.length>0&&(i.iconName=_d(i.prefix,t.innerText)||Fr(i.prefix,No(t.innerText))),!i.iconName&&b.autoFetchSvg&&t.firstChild&&t.firstChild.nodeType===Node.TEXT_NODE&&(i.iconName=t.firstChild.data)),i}function qd(t){var r=He(t.attributes).reduce(function(e,n){return e.name!=="class"&&e.name!=="style"&&(e[n.name]=n.value),e},{});return r}function Qd(){return{iconName:null,prefix:null,transform:Q,symbol:!1,mask:{iconName:null,prefix:null,rest:[]},maskId:null,extra:{classes:[],styles:{},attributes:{}}}}function Ua(t){var r=arguments.length>1&&arguments[1]!==void 0?arguments[1]:{styleParser:!0},e=Jd(t),n=e.iconName,i=e.prefix,a=e.rest,o=qd(t),s=gr("parseNodeAttributes",{},t),c=r.styleParser?Xd(t):[];return m({iconName:n,prefix:i,transform:Q,mask:{iconName:null,prefix:null,rest:[]},maskId:null,symbol:!1,extra:{classes:a,styles:c,attributes:o}},s)}var ef=K.styles;function Xo(t){var r=b.autoReplaceSvg==="nest"?Ua(t,{styleParser:!1}):Ua(t);return~r.extra.classes.indexOf(Mo)?ge("generateLayersText",t,r):ge("generateSvgReplacementMutation",t,r)}function tf(){return[].concat(Z(_o),Z(Eo))}function za(t){var r=arguments.length>1&&arguments[1]!==void 0?arguments[1]:null;if(!se)return Promise.resolve();var e=M.documentElement.classList,n=function(d){return e.add("".concat(Ca,"-").concat(d))},i=function(d){return e.remove("".concat(Ca,"-").concat(d))},a=b.autoFetchSvg?tf():to.concat(Object.keys(ef));a.includes("fa")||a.push("fa");var o=[".".concat(Mo,":not([").concat(we,"])")].concat(a.map(function(f){return".".concat(f,":not([").concat(we,"])")})).join(", ");if(o.length===0)return Promise.resolve();var s=[];try{s=He(t.querySelectorAll(o))}catch{}if(s.length>0)n("pending"),i("complete");else return Promise.resolve();var c=Or.begin("onTree"),u=s.reduce(function(f,d){try{var p=Xo(d);p&&f.push(p)}catch(h){To||h.name==="MissingIcon"&&console.error(h)}return f},[]);return new Promise(function(f,d){Promise.all(u).then(function(p){Ko(p,function(){n("active"),n("complete"),i("pending"),typeof r=="function"&&r(),c(),f()})}).catch(function(p){c(),d(p)})})}function nf(t){var r=arguments.length>1&&arguments[1]!==void 0?arguments[1]:null;Xo(t).then(function(e){e&&Ko([e],r)})}function rf(t){return function(r){var e=arguments.length>1&&arguments[1]!==void 0?arguments[1]:{},n=(r||{}).icon?r:br(r||{}),i=e.mask;return i&&(i=(i||{}).icon?i:br(i||{})),t(n,m(m({},e),{},{mask:i}))}}var af=function(r){var e=arguments.length>1&&arguments[1]!==void 0?arguments[1]:{},n=e.transform,i=n===void 0?Q:n,a=e.symbol,o=a===void 0?!1:a,s=e.mask,c=s===void 0?null:s,u=e.maskId,f=u===void 0?null:u,d=e.classes,p=d===void 0?[]:d,h=e.attributes,E=h===void 0?{}:h,_=e.styles,v=_===void 0?{}:_;if(r){var y=r.prefix,A=r.iconName,x=r.icon;return ln(m({type:"icon"},r),function(){return Ae("beforeDOMElementCreation",{iconDefinition:r,params:e}),Rr({icons:{main:vr(x),mask:c?vr(c.icon):{found:!1,width:null,height:null,icon:{}}},prefix:y,iconName:A,transform:m(m({},Q),i),symbol:o,maskId:f,extra:{attributes:E,styles:v,classes:p}})})}},of={mixout:function(){return{icon:rf(af)}},hooks:function(){return{mutationObserverCallbacks:function(e){return e.treeCallback=za,e.nodeCallback=nf,e}}},provides:function(r){r.i2svg=function(e){var n=e.node,i=n===void 0?M:n,a=e.callback,o=a===void 0?function(){}:a;return za(i,o)},r.generateSvgReplacementMutation=function(e,n){var i=n.iconName,a=n.prefix,o=n.transform,s=n.symbol,c=n.mask,u=n.maskId,f=n.extra;return new Promise(function(d,p){Promise.all([yr(i,a),c.iconName?yr(c.iconName,c.prefix):Promise.resolve({found:!1,width:512,height:512,icon:{}})]).then(function(h){var E=on(h,2),_=E[0],v=E[1];d([e,Rr({icons:{main:_,mask:v},prefix:a,iconName:i,transform:o,symbol:s,maskId:u,extra:f,watchable:!0})])}).catch(p)})},r.generateAbstractIcon=function(e){var n=e.children,i=e.attributes,a=e.main,o=e.transform,s=e.styles,c=sn(s);c.length>0&&(i.style=c);var u;return Mr(o)&&(u=ge("generateAbstractTransformGrouping",{main:a,transform:o,containerWidth:a.width,iconWidth:a.width})),n.push(u||a.icon),{children:n,attributes:i}}}},sf={mixout:function(){return{layer:function(e){var n=arguments.length>1&&arguments[1]!==void 0?arguments[1]:{},i=n.classes,a=i===void 0?[]:i;return ln({type:"layer"},function(){Ae("beforeDOMElementCreation",{assembler:e,params:n});var o=[];return e(function(s){Array.isArray(s)?s.map(function(c){o=o.concat(c.abstract)}):o=o.concat(s.abstract)}),[{tag:"span",attributes:{class:["".concat(b.cssPrefix,"-layers")].concat(Z(a)).join(" ")},children:o}]})}}}},cf={mixout:function(){return{counter:function(e){var n=arguments.length>1&&arguments[1]!==void 0?arguments[1]:{},i=n.title,a=i===void 0?null:i,o=n.classes,s=o===void 0?[]:o,c=n.attributes,u=c===void 0?{}:c,f=n.styles,d=f===void 0?{}:f;return ln({type:"counter",content:e},function(){return Ae("beforeDOMElementCreation",{content:e,params:n}),jd({content:e.toString(),title:a,extra:{attributes:u,styles:d,classes:["".concat(b.cssPrefix,"-layers-counter")].concat(Z(s))}})})}}}},uf={mixout:function(){return{text:function(e){var n=arguments.length>1&&arguments[1]!==void 0?arguments[1]:{},i=n.transform,a=i===void 0?Q:i,o=n.classes,s=o===void 0?[]:o,c=n.attributes,u=c===void 0?{}:c,f=n.styles,d=f===void 0?{}:f;return ln({type:"text",content:e},function(){return Ae("beforeDOMElementCreation",{content:e,params:n}),Na({content:e,transform:m(m({},Q),a),extra:{attributes:u,styles:d,classes:["".concat(b.cssPrefix,"-layers-text")].concat(Z(s))}})})}}},provides:function(r){r.generateLayersText=function(e,n){var i=n.transform,a=n.extra,o=null,s=null;if(Qa){var c=parseInt(getComputedStyle(e).fontSize,10),u=e.getBoundingClientRect();o=u.width/c,s=u.height/c}return Promise.resolve([e,Na({content:e.innerHTML,width:o,height:s,transform:i,extra:a,watchable:!0})])}}},Jo=new RegExp('"',"ug"),$a=[1105920,1112319],Ha=m(m(m(m({},{FontAwesome:{normal:"fas",400:"fas"}}),$u),Zl),Ju),Er=Object.keys(Ha).reduce(function(t,r){return t[r.toLowerCase()]=Ha[r],t},{}),lf=Object.keys(Er).reduce(function(t,r){var e=Er[r];return t[r]=e[900]||Z(Object.entries(e))[0][1],t},{});function df(t){var r=t.replace(Jo,"");return No(Z(r)[0]||"")}function ff(t){var r=t.getPropertyValue("font-feature-settings").includes("ss01"),e=t.getPropertyValue("content"),n=e.replace(Jo,""),i=n.codePointAt(0),a=i>=$a[0]&&i<=$a[1],o=n.length===2?n[0]===n[1]:!1;return a||o||r}function mf(t,r){var e=t.replace(/^['"]|['"]$/g,"").toLowerCase(),n=parseInt(r),i=isNaN(n)?"normal":n;return(Er[e]||{})[i]||lf[e]}function Va(t,r){var e="".concat(Xl).concat(r.replace(":","-"));return new Promise(function(n,i){if(t.getAttribute(e)!==null)return n();var a=He(t.children),o=a.filter(function(ee){return ee.getAttribute(dr)===r})[0],s=he.getComputedStyle(t,r),c=s.getPropertyValue("font-family"),u=c.match(td),f=s.getPropertyValue("font-weight"),d=s.getPropertyValue("content");if(o&&!u)return t.removeChild(o),n();if(u&&d!=="none"&&d!==""){var p=s.getPropertyValue("content"),h=mf(c,f),E=df(p),_=u[0].startsWith("FontAwesome"),v=ff(s),y=Fr(h,E),A=y;if(_){var x=Ed(E);x.iconName&&x.prefix&&(y=x.iconName,h=x.prefix)}if(y&&!v&&(!o||o.getAttribute(Cr)!==h||o.getAttribute(Ir)!==A)){t.setAttribute(e,A),o&&t.removeChild(o);var T=Qd(),k=T.extra;k.attributes[dr]=r,yr(y,h).then(function(ee){var te=Rr(m(m({},T),{},{icons:{main:ee,mask:Vo()},prefix:h,iconName:A,extra:k,watchable:!0})),An=M.createElementNS("http://www.w3.org/2000/svg","svg");r==="::before"?t.insertBefore(An,t.firstChild):t.appendChild(An),An.outerHTML=te.map(function($s){return vt($s)}).join(`
`),t.removeAttribute(e),n()}).catch(i)}else n()}else n()})}function hf(t){return Promise.all([Va(t,"::before"),Va(t,"::after")])}function pf(t){return t.parentNode!==document.head&&!~ql.indexOf(t.tagName.toUpperCase())&&!t.getAttribute(dr)&&(!t.parentNode||t.parentNode.tagName!=="svg")}var gf=function(r){return!!r&&Io.some(function(e){return r.includes(e)})},bf=function(r){if(!r)return[];var e=new Set,n=r.split(/,(?![^()]*\))/).map(function(c){return c.trim()});n=n.flatMap(function(c){return c.includes("(")?c:c.split(",").map(function(u){return u.trim()})});var i=Qt(n),a;try{for(i.s();!(a=i.n()).done;){var o=a.value;if(gf(o)){var s=Io.reduce(function(c,u){return c.replace(u,"")},o);s!==""&&s!=="*"&&e.add(s)}}}catch(c){i.e(c)}finally{i.f()}return e};function Wa(t){var r=arguments.length>1&&arguments[1]!==void 0?arguments[1]:!1;if(se){var e;if(r)e=t;else if(b.searchPseudoElementsFullScan)e=t.querySelectorAll("*");else{var n=new Set,i=Qt(document.styleSheets),a;try{for(i.s();!(a=i.n()).done;){var o=a.value;try{var s=Qt(o.cssRules),c;try{for(s.s();!(c=s.n()).done;){var u=c.value,f=bf(u.selectorText),d=Qt(f),p;try{for(d.s();!(p=d.n()).done;){var h=p.value;n.add(h)}}catch(_){d.e(_)}finally{d.f()}}}catch(_){s.e(_)}finally{s.f()}}catch(_){b.searchPseudoElementsWarnings&&console.warn("Font Awesome: cannot parse stylesheet: ".concat(o.href," (").concat(_.message,`)
If it declares any Font Awesome CSS pseudo-elements, they will not be rendered as SVG icons. Add crossorigin="anonymous" to the <link>, enable searchPseudoElementsFullScan for slower but more thorough DOM parsing, or suppress this warning by setting searchPseudoElementsWarnings to false.`))}}}catch(_){i.e(_)}finally{i.f()}if(!n.size)return;var E=Array.from(n).join(", ");try{e=t.querySelectorAll(E)}catch{}}return new Promise(function(_,v){var y=He(e).filter(pf).map(hf),A=Or.begin("searchPseudoElements");Zo(),Promise.all(y).then(function(){A(),_r(),_()}).catch(function(){A(),_r(),v()})})}}var vf={hooks:function(){return{mutationObserverCallbacks:function(e){return e.pseudoElementsCallback=Wa,e}}},provides:function(r){r.pseudoElements2svg=function(e){var n=e.node,i=n===void 0?M:n;b.searchPseudoElements&&Wa(i)}}},Ga=!1,yf={mixout:function(){return{dom:{unwatch:function(){Zo(),Ga=!0}}}},hooks:function(){return{bootstrap:function(){ja(gr("mutationObserverCallbacks",{}))},noAuto:function(){Zd()},watch:function(e){var n=e.observeMutationsRoot;Ga?_r():ja(gr("mutationObserverCallbacks",{observeMutationsRoot:n}))}}}},Ya=function(r){var e={size:16,x:0,y:0,flipX:!1,flipY:!1,rotate:0};return r.toLowerCase().split(" ").reduce(function(n,i){var a=i.toLowerCase().split("-"),o=a[0],s=a.slice(1).join("-");if(o&&s==="h")return n.flipX=!0,n;if(o&&s==="v")return n.flipY=!0,n;if(s=parseFloat(s),isNaN(s))return n;switch(o){case"grow":n.size=n.size+s;break;case"shrink":n.size=n.size-s;break;case"left":n.x=n.x-s;break;case"right":n.x=n.x+s;break;case"up":n.y=n.y-s;break;case"down":n.y=n.y+s;break;case"rotate":n.rotate=n.rotate+s;break}return n},e)},Df={mixout:function(){return{parse:{transform:function(e){return Ya(e)}}}},hooks:function(){return{parseNodeAttributes:function(e,n){var i=n.getAttribute("data-fa-transform");return i&&(e.transform=Ya(i)),e}}},provides:function(r){r.generateAbstractTransformGrouping=function(e){var n=e.main,i=e.transform,a=e.containerWidth,o=e.iconWidth,s={transform:"translate(".concat(a/2," 256)")},c="translate(".concat(i.x*32,", ").concat(i.y*32,") "),u="scale(".concat(i.size/16*(i.flipX?-1:1),", ").concat(i.size/16*(i.flipY?-1:1),") "),f="rotate(".concat(i.rotate," 0 0)"),d={transform:"".concat(c," ").concat(u," ").concat(f)},p={transform:"translate(".concat(o/2*-1," -256)")},h={outer:s,inner:d,path:p};return{tag:"g",attributes:m({},h.outer),children:[{tag:"g",attributes:m({},h.inner),children:[{tag:n.icon.tag,children:n.icon.children,attributes:m(m({},n.icon.attributes),h.path)}]}]}}}},sr={x:0,y:0,width:"100%",height:"100%"};function Ka(t){var r=arguments.length>1&&arguments[1]!==void 0?arguments[1]:!0;return t.attributes&&(t.attributes.fill||r)&&(t.attributes.fill="black"),t}function _f(t){return t.tag==="g"?t.children:[t]}var Ef={hooks:function(){return{parseNodeAttributes:function(e,n){var i=n.getAttribute("data-fa-mask"),a=i?un(i.split(" ").map(function(o){return o.trim()})):Vo();return a.prefix||(a.prefix=pe()),e.mask=a,e.maskId=n.getAttribute("data-fa-mask-id"),e}}},provides:function(r){r.generateAbstractMask=function(e){var n=e.children,i=e.attributes,a=e.main,o=e.mask,s=e.maskId,c=e.transform,u=a.width,f=a.icon,d=o.width,p=o.icon,h=dd({transform:c,containerWidth:d,iconWidth:u}),E={tag:"rect",attributes:m(m({},sr),{},{fill:"white"})},_=f.children?{children:f.children.map(Ka)}:{},v={tag:"g",attributes:m({},h.inner),children:[Ka(m({tag:f.tag,attributes:m(m({},f.attributes),h.path)},_))]},y={tag:"g",attributes:m({},h.outer),children:[v]},A="mask-".concat(s||Sa()),x="clip-".concat(s||Sa()),T={tag:"mask",attributes:m(m({},sr),{},{id:A,maskUnits:"userSpaceOnUse",maskContentUnits:"userSpaceOnUse"}),children:[E,y]},k={tag:"defs",children:[{tag:"clipPath",attributes:{id:x},children:_f(p)},T]};return n.push(k,{tag:"rect",attributes:m({fill:"currentColor","clip-path":"url(#".concat(x,")"),mask:"url(#".concat(A,")")},sr)}),{children:n,attributes:i}}}},wf={provides:function(r){var e=!1;he.matchMedia&&(e=he.matchMedia("(prefers-reduced-motion: reduce)").matches),r.missingIconAbstract=function(){var n=[],i={fill:"currentColor"},a={attributeType:"XML",repeatCount:"indefinite",dur:"2s"};n.push({tag:"path",attributes:m(m({},i),{},{d:"M156.5,447.7l-12.6,29.5c-18.7-9.5-35.9-21.2-51.5-34.9l22.7-22.7C127.6,430.5,141.5,440,156.5,447.7z M40.6,272H8.5 c1.4,21.2,5.4,41.7,11.7,61.1L50,321.2C45.1,305.5,41.8,289,40.6,272z M40.6,240c1.4-18.8,5.2-37,11.1-54.1l-29.5-12.6 C14.7,194.3,10,216.7,8.5,240H40.6z M64.3,156.5c7.8-14.9,17.2-28.8,28.1-41.5L69.7,92.3c-13.7,15.6-25.5,32.8-34.9,51.5 L64.3,156.5z M397,419.6c-13.9,12-29.4,22.3-46.1,30.4l11.9,29.8c20.7-9.9,39.8-22.6,56.9-37.6L397,419.6z M115,92.4 c13.9-12,29.4-22.3,46.1-30.4l-11.9-29.8c-20.7,9.9-39.8,22.6-56.8,37.6L115,92.4z M447.7,355.5c-7.8,14.9-17.2,28.8-28.1,41.5 l22.7,22.7c13.7-15.6,25.5-32.9,34.9-51.5L447.7,355.5z M471.4,272c-1.4,18.8-5.2,37-11.1,54.1l29.5,12.6 c7.5-21.1,12.2-43.5,13.6-66.8H471.4z M321.2,462c-15.7,5-32.2,8.2-49.2,9.4v32.1c21.2-1.4,41.7-5.4,61.1-11.7L321.2,462z M240,471.4c-18.8-1.4-37-5.2-54.1-11.1l-12.6,29.5c21.1,7.5,43.5,12.2,66.8,13.6V471.4z M462,190.8c5,15.7,8.2,32.2,9.4,49.2h32.1 c-1.4-21.2-5.4-41.7-11.7-61.1L462,190.8z M92.4,397c-12-13.9-22.3-29.4-30.4-46.1l-29.8,11.9c9.9,20.7,22.6,39.8,37.6,56.9 L92.4,397z M272,40.6c18.8,1.4,36.9,5.2,54.1,11.1l12.6-29.5C317.7,14.7,295.3,10,272,8.5V40.6z M190.8,50 c15.7-5,32.2-8.2,49.2-9.4V8.5c-21.2,1.4-41.7,5.4-61.1,11.7L190.8,50z M442.3,92.3L419.6,115c12,13.9,22.3,29.4,30.5,46.1 l29.8-11.9C470,128.5,457.3,109.4,442.3,92.3z M397,92.4l22.7-22.7c-15.6-13.7-32.8-25.5-51.5-34.9l-12.6,29.5 C370.4,72.1,384.4,81.5,397,92.4z"})});var o=m(m({},a),{},{attributeName:"opacity"}),s={tag:"circle",attributes:m(m({},i),{},{cx:"256",cy:"364",r:"28"}),children:[]};return e||s.children.push({tag:"animate",attributes:m(m({},a),{},{attributeName:"r",values:"28;14;28;28;14;28;"})},{tag:"animate",attributes:m(m({},o),{},{values:"1;0;1;1;0;1;"})}),n.push(s),n.push({tag:"path",attributes:m(m({},i),{},{opacity:"1",d:"M263.7,312h-16c-6.6,0-12-5.4-12-12c0-71,77.4-63.9,77.4-107.8c0-20-17.8-40.2-57.4-40.2c-29.1,0-44.3,9.6-59.2,28.7 c-3.9,5-11.1,6-16.2,2.4l-13.1-9.2c-5.6-3.9-6.9-11.8-2.6-17.2c21.2-27.2,46.4-44.7,91.2-44.7c52.3,0,97.4,29.8,97.4,80.2 c0,67.6-77.4,63.5-77.4,107.8C275.7,306.6,270.3,312,263.7,312z"}),children:e?[]:[{tag:"animate",attributes:m(m({},o),{},{values:"1;0;0;0;0;1;"})}]}),e||n.push({tag:"path",attributes:m(m({},i),{},{opacity:"0",d:"M232.5,134.5l7,168c0.3,6.4,5.6,11.5,12,11.5h9c6.4,0,11.7-5.1,12-11.5l7-168c0.3-6.8-5.2-12.5-12-12.5h-23 C237.7,122,232.2,127.7,232.5,134.5z"}),children:[{tag:"animate",attributes:m(m({},o),{},{values:"0;0;1;1;0;0;"})}]}),{tag:"g",attributes:{class:"missing"},children:n}}}},Af={hooks:function(){return{parseNodeAttributes:function(e,n){var i=n.getAttribute("data-fa-symbol"),a=i===null?!1:i===""?!0:i;return e.symbol=a,e}}}},Cf=[hd,of,sf,cf,uf,vf,yf,Df,Ef,wf,Af];Fd(Cf,{mixoutsTo:G});var rb=G.noAuto,qo=G.config,ib=G.library,Qo=G.dom,es=G.parse,ab=G.findIconDefinition,ob=G.toHtml,ts=G.icon,sb=G.layer,If=G.text,Tf=G.counter;var Sf=["*"],Mf=(()=>{class t{defaultPrefix="fas";fallbackIcon=null;fixedWidth;set autoAddCss(e){qo.autoAddCss=e,this._autoAddCss=e}get autoAddCss(){return this._autoAddCss}_autoAddCss=!0;static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})(),xf=(()=>{class t{definitions={};addIcons(...e){for(let n of e){n.prefix in this.definitions||(this.definitions[n.prefix]={}),this.definitions[n.prefix][n.iconName]=n;for(let i of n.icon[2])typeof i=="string"&&(this.definitions[n.prefix][i]=n)}}addIconPacks(...e){for(let n of e){let i=Object.keys(n).map(a=>n[a]);this.addIcons(...i)}}getIconDefinition(e,n){return e in this.definitions&&n in this.definitions[e]?this.definitions[e][n]:null}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})(),Ff=t=>{throw new Error(`Could not find icon with iconName=${t.iconName} and prefix=${t.prefix} in the icon library.`)},Rf=()=>{throw new Error("Property `icon` is required for `fa-icon`/`fa-duotone-icon` components.")},rs=t=>t!=null&&(t===90||t===180||t===270||t==="90"||t==="180"||t==="270"),Of=t=>{let r=rs(t.rotate),e={[`fa-${t.animation}`]:t.animation!=null&&!t.animation.startsWith("spin"),"fa-spin":t.animation==="spin"||t.animation==="spin-reverse","fa-spin-pulse":t.animation==="spin-pulse"||t.animation==="spin-pulse-reverse","fa-spin-reverse":t.animation==="spin-reverse"||t.animation==="spin-pulse-reverse","fa-pulse":t.animation==="spin-pulse"||t.animation==="spin-pulse-reverse","fa-fw":t.fixedWidth,"fa-border":t.border,"fa-inverse":t.inverse,"fa-layers-counter":t.counter,"fa-flip-horizontal":t.flip==="horizontal"||t.flip==="both","fa-flip-vertical":t.flip==="vertical"||t.flip==="both",[`fa-${t.size}`]:t.size!==null,[`fa-rotate-${t.rotate}`]:r,"fa-rotate-by":t.rotate!=null&&!r,[`fa-pull-${t.pull}`]:t.pull!==null,[`fa-stack-${t.stackItemSize}`]:t.stackItemSize!=null};return Object.keys(e).map(n=>e[n]?n:null).filter(n=>n!=null)},Nr=new WeakSet,ns="fa-auto-css";function kf(t,r){if(!r.autoAddCss||Nr.has(t))return;if(t.getElementById(ns)!=null){r.autoAddCss=!1,Nr.add(t);return}let e=t.createElement("style");e.setAttribute("type","text/css"),e.setAttribute("id",ns),e.innerHTML=Qo.css();let n=t.head.childNodes,i=null;for(let a=n.length-1;a>-1;a--){let o=n[a],s=o.nodeName.toUpperCase();["STYLE","LINK"].indexOf(s)>-1&&(i=o)}t.head.insertBefore(e,i),r.autoAddCss=!1,Nr.add(t)}var Nf=t=>t.prefix!==void 0&&t.iconName!==void 0,Pf=(t,r)=>Nf(t)?t:Array.isArray(t)&&t.length===2?{prefix:t[0],iconName:t[1]}:{prefix:r,iconName:t},Lf=(()=>{class t{stackItemSize=Bt("1x");size=Bt();_effect=Ot(()=>{if(this.size())throw new Error('fa-icon is not allowed to customize size when used inside fa-stack. Set size on the enclosing fa-stack instead: <fa-stack size="4x">...</fa-stack>.')});static \u0275fac=function(n){return new(n||t)};static \u0275dir=B({type:t,selectors:[["fa-icon","stackItemSize",""],["fa-duotone-icon","stackItemSize",""]],inputs:{stackItemSize:[1,"stackItemSize"],size:[1,"size"]}})}return t})(),Bf=(()=>{class t{size=Bt();classes=kn(()=>{let e=this.size(),n=e?{[`fa-${e}`]:!0}:{};return It(R({},n),{"fa-stack":!0})});static \u0275fac=function(n){return new(n||t)};static \u0275cmp=V({type:t,selectors:[["fa-stack"]],hostVars:2,hostBindings:function(n,i){n&2&&Lt(i.classes())},inputs:{size:[1,"size"]},ngContentSelectors:Sf,decls:1,vars:0,template:function(n,i){n&1&&(Oe(),le(0))},encapsulation:2,changeDetection:0})}return t})(),bb=(()=>{class t{icon=z();title=z();animation=z();mask=z();flip=z();size=z();pull=z();border=z();inverse=z();symbol=z();rotate=z();fixedWidth=z();transform=z();a11yRole=z();renderedIconHTML=kn(()=>{let e=this.icon()??this.config.fallbackIcon;if(!e)return Rf(),"";let n=this.findIconDefinition(e);if(!n)return"";let i=this.buildParams();kf(this.document,this.config);let a=ts(n,i);return this.sanitizer.bypassSecurityTrustHtml(a.html.join(`
`))});document=l(I);sanitizer=l(dt);config=l(Mf);iconLibrary=l(xf);stackItem=l(Lf,{optional:!0});stack=l(Bf,{optional:!0});constructor(){this.stack!=null&&this.stackItem==null&&console.error('FontAwesome: fa-icon and fa-duotone-icon elements must specify stackItemSize attribute when wrapped into fa-stack. Example: <fa-icon stackItemSize="2x" />.')}findIconDefinition(e){let n=Pf(e,this.config.defaultPrefix);if("icon"in n)return n;let i=this.iconLibrary.getIconDefinition(n.prefix,n.iconName);return i??(Ff(n),null)}buildParams(){let e=this.fixedWidth(),n={flip:this.flip(),animation:this.animation(),border:this.border(),inverse:this.inverse(),size:this.size(),pull:this.pull(),rotate:this.rotate(),fixedWidth:typeof e=="boolean"?e:this.config.fixedWidth,stackItemSize:this.stackItem!=null?this.stackItem.stackItemSize():void 0},i=this.transform(),a=typeof i=="string"?es.transform(i):i,o=this.mask(),s=o!=null?this.findIconDefinition(o):null,c={},u=this.a11yRole();u!=null&&(c.role=u);let f={};return n.rotate!=null&&!rs(n.rotate)&&(f["--fa-rotate-angle"]=`${n.rotate}`),{title:this.title(),transform:a,classes:Of(n),mask:s??void 0,symbol:this.symbol(),attributes:c,styles:f}}static \u0275fac=function(n){return new(n||t)};static \u0275cmp=V({type:t,selectors:[["fa-icon"]],hostAttrs:[1,"ng-fa-icon"],hostVars:2,hostBindings:function(n,i){n&2&&(ki("innerHTML",i.renderedIconHTML(),Ti),et("title",i.title()??void 0))},inputs:{icon:[1,"icon"],title:[1,"title"],animation:[1,"animation"],mask:[1,"mask"],flip:[1,"flip"],size:[1,"size"],pull:[1,"pull"],border:[1,"border"],inverse:[1,"inverse"],symbol:[1,"symbol"],rotate:[1,"rotate"],fixedWidth:[1,"fixedWidth"],transform:[1,"transform"],a11yRole:[1,"a11yRole"]},outputs:{icon:"iconChange",title:"titleChange",animation:"animationChange",mask:"maskChange",flip:"flipChange",size:"sizeChange",pull:"pullChange",border:"borderChange",inverse:"inverseChange",symbol:"symbolChange",rotate:"rotateChange",fixedWidth:"fixedWidthChange",transform:"transformChange",a11yRole:"a11yRoleChange"},decls:0,vars:0,template:function(n,i){},encapsulation:2,changeDetection:0})}return t})();var vb=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275mod=L({type:t});static \u0275inj=P({})}return t})();var Pr;try{Pr=typeof Intl<"u"&&Intl.v8BreakIterator}catch{Pr=!1}var j=(()=>{class t{_platformId=l(Se);isBrowser=this._platformId?Xi(this._platformId):typeof document=="object"&&!!document;EDGE=this.isBrowser&&/(edge)/i.test(navigator.userAgent);TRIDENT=this.isBrowser&&/(msie|trident)/i.test(navigator.userAgent);BLINK=this.isBrowser&&!!(window.chrome||Pr)&&typeof CSS<"u"&&!this.EDGE&&!this.TRIDENT;WEBKIT=this.isBrowser&&/AppleWebKit/i.test(navigator.userAgent)&&!this.BLINK&&!this.EDGE&&!this.TRIDENT;IOS=this.isBrowser&&/iPad|iPhone|iPod/.test(navigator.userAgent)&&!("MSStream"in window);FIREFOX=this.isBrowser&&/(firefox|minefield)/i.test(navigator.userAgent);ANDROID=this.isBrowser&&/android/i.test(navigator.userAgent)&&!this.TRIDENT;SAFARI=this.isBrowser&&/safari/i.test(navigator.userAgent)&&this.WEBKIT;constructor(){}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();function Lr(t){return Array.isArray(t)?t:[t]}var is=new Set,Ce,dn=(()=>{class t{_platform=l(j);_nonce=l(Je,{optional:!0});_matchMedia;constructor(){this._matchMedia=this._platform.isBrowser&&window.matchMedia?window.matchMedia.bind(window):Uf}matchMedia(e){return(this._platform.WEBKIT||this._platform.BLINK)&&jf(e,this._nonce),this._matchMedia(e)}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();function jf(t,r){if(!is.has(t))try{Ce||(Ce=document.createElement("style"),r&&Ce.setAttribute("nonce",r),Ce.setAttribute("type","text/css"),document.head.appendChild(Ce)),Ce.sheet&&(Ce.sheet.insertRule(`@media ${t} {body{ }}`,0),is.add(t))}catch(e){console.error(e)}}function Uf(t){return{matches:t==="all"||t==="",media:t,addListener:()=>{},removeListener:()=>{}}}var Br=(()=>{class t{_mediaMatcher=l(dn);_zone=l(O);_queries=new Map;_destroySubject=new $;constructor(){}ngOnDestroy(){this._destroySubject.next(),this._destroySubject.complete()}isMatched(e){return as(Lr(e)).some(i=>this._registerQuery(i).mql.matches)}observe(e){let i=as(Lr(e)).map(o=>this._registerQuery(o).observable),a=ii(i);return a=ai(a.pipe(si(1)),a.pipe(xt(1),Mt(0))),a.pipe(J(o=>{let s={matches:!1,breakpoints:{}};return o.forEach(({matches:c,query:u})=>{s.matches=s.matches||c,s.breakpoints[u]=c}),s}))}_registerQuery(e){if(this._queries.has(e))return this._queries.get(e);let n=this._mediaMatcher.matchMedia(e),a={observable:new Tt(o=>{let s=c=>this._zone.run(()=>o.next(c));return n.addListener(s),()=>{n.removeListener(s)}}).pipe(li(n),J(({matches:o})=>({query:e,matches:o})),Ft(this._destroySubject)),mql:n};return this._queries.set(e,a),a}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();function as(t){return t.map(r=>r.split(",")).reduce((r,e)=>r.concat(e)).map(r=>r.trim())}var Ob={XSmall:"(max-width: 599.98px)",Small:"(min-width: 600px) and (max-width: 959.98px)",Medium:"(min-width: 960px) and (max-width: 1279.98px)",Large:"(min-width: 1280px) and (max-width: 1919.98px)",XLarge:"(min-width: 1920px)",Handset:"(max-width: 599.98px) and (orientation: portrait), (max-width: 959.98px) and (orientation: landscape)",Tablet:"(min-width: 600px) and (max-width: 839.98px) and (orientation: portrait), (min-width: 960px) and (max-width: 1279.98px) and (orientation: landscape)",Web:"(min-width: 840px) and (orientation: portrait), (min-width: 1280px) and (orientation: landscape)",HandsetPortrait:"(max-width: 599.98px) and (orientation: portrait)",TabletPortrait:"(min-width: 600px) and (max-width: 839.98px) and (orientation: portrait)",WebPortrait:"(min-width: 840px) and (orientation: portrait)",HandsetLandscape:"(max-width: 959.98px) and (orientation: landscape)",TabletLandscape:"(min-width: 960px) and (max-width: 1279.98px) and (orientation: landscape)",WebLandscape:"(min-width: 1280px) and (orientation: landscape)"};var zf=new C("MATERIAL_ANIMATIONS"),os=null;function $f(){return l(zf,{optional:!0})?.animationsDisabled||l(vi,{optional:!0})==="NoopAnimations"?"di-disabled":(os??=l(dn).matchMedia("(prefers-reduced-motion)").matches,os?"reduced-motion":"enabled")}function Ve(){return $f()!=="enabled"}function Hf(t,r=0){return ss(t)?Number(t):arguments.length===2?r:0}function ss(t){return!isNaN(parseFloat(t))&&!isNaN(Number(t))}function be(t){return t instanceof H?t.nativeElement:t}function cs(t,...r){return r.length?r.some(e=>t[e]):t.altKey||t.shiftKey||t.ctrlKey||t.metaKey}var jr={},Ur=class t{_appId=l(ye);static _infix=`a${Math.floor(Math.random()*1e5).toString()}`;getId(r,e=!1){return this._appId!=="ng"&&(r+=this._appId),jr.hasOwnProperty(r)||(jr[r]=0),`${r}${e?t._infix+"-":""}${jr[r]++}`}static \u0275fac=function(e){return new(e||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})};function yt(t){return t.buttons===0||t.detail===0}function Dt(t){let r=t.touches&&t.touches[0]||t.changedTouches&&t.changedTouches[0];return!!r&&r.identifier===-1&&(r.radiusX==null||r.radiusX===1)&&(r.radiusY==null||r.radiusY===1)}var zr;function us(){if(zr==null){let t=typeof document<"u"?document.head:null;zr=!!(t&&(t.createShadowRoot||t.attachShadow))}return zr}function $r(t){if(us()){let r=t.getRootNode?t.getRootNode():null;if(typeof ShadowRoot<"u"&&ShadowRoot&&r instanceof ShadowRoot)return r}return null}function Hr(){let t=typeof document<"u"&&document?document.activeElement:null;for(;t&&t.shadowRoot;){let r=t.shadowRoot.activeElement;if(r===t)break;t=r}return t}function X(t){return t.composedPath?t.composedPath()[0]:t.target}var _t;function ls(){if(_t==null&&typeof window<"u")try{window.addEventListener("test",null,Object.defineProperty({},"passive",{get:()=>_t=!0}))}finally{_t=_t||!1}return _t}function We(t){return ls()?t:!!t.capture}var ds=new C("cdk-input-modality-detector-options"),fs={ignoreKeys:[18,17,224,91,16]},ms=650,Vr={passive:!0,capture:!0},hs=(()=>{class t{_platform=l(j);_listenerCleanups;modalityDetected;modalityChanged;get mostRecentModality(){return this._modality.value}_mostRecentTarget=null;_modality=new ri(null);_options;_lastTouchMs=0;_onKeydown=e=>{this._options?.ignoreKeys?.some(n=>n===e.keyCode)||(this._modality.next("keyboard"),this._mostRecentTarget=X(e))};_onMousedown=e=>{Date.now()-this._lastTouchMs<ms||(this._modality.next(yt(e)?"keyboard":"mouse"),this._mostRecentTarget=X(e))};_onTouchstart=e=>{if(Dt(e)){this._modality.next("keyboard");return}this._lastTouchMs=Date.now(),this._modality.next("touch"),this._mostRecentTarget=X(e)};constructor(){let e=l(O),n=l(I),i=l(ds,{optional:!0});if(this._options=R(R({},fs),i),this.modalityDetected=this._modality.pipe(xt(1)),this.modalityChanged=this.modalityDetected.pipe(ci()),this._platform.isBrowser){let a=l(Fe).createRenderer(null,null);this._listenerCleanups=e.runOutsideAngular(()=>[a.listen(n,"keydown",this._onKeydown,Vr),a.listen(n,"mousedown",this._onMousedown,Vr),a.listen(n,"touchstart",this._onTouchstart,Vr)])}}ngOnDestroy(){this._modality.complete(),this._listenerCleanups?.forEach(e=>e())}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})(),Et=(function(t){return t[t.IMMEDIATE=0]="IMMEDIATE",t[t.EVENTUAL=1]="EVENTUAL",t})(Et||{}),ps=new C("cdk-focus-monitor-default-options"),fn=We({passive:!0,capture:!0}),mn=(()=>{class t{_ngZone=l(O);_platform=l(j);_inputModalityDetector=l(hs);_origin=null;_lastFocusOrigin=null;_windowFocused=!1;_windowFocusTimeoutId;_originTimeoutId;_originFromTouchInteraction=!1;_elementInfo=new Map;_monitoredElementCount=0;_rootNodeFocusListenerCount=new Map;_detectionMode;_windowFocusListener=()=>{this._windowFocused=!0,this._windowFocusTimeoutId=setTimeout(()=>this._windowFocused=!1)};_document=l(I);_stopInputModalityDetector=new $;constructor(){let e=l(ps,{optional:!0});this._detectionMode=e?.detectionMode||Et.IMMEDIATE}_rootNodeFocusAndBlurListener=e=>{let n=X(e);for(let i=n;i;i=i.parentElement)e.type==="focus"?this._onFocus(e,i):this._onBlur(e,i)};monitor(e,n=!1){let i=be(e);if(!this._platform.isBrowser||i.nodeType!==1)return Ke();let a=$r(i)||this._document,o=this._elementInfo.get(i);if(o)return n&&(o.checkChildren=!0),o.subject;let s={checkChildren:n,subject:new $,rootNode:a};return this._elementInfo.set(i,s),this._registerGlobalListeners(s),s.subject}stopMonitoring(e){let n=be(e),i=this._elementInfo.get(n);i&&(i.subject.complete(),this._setClasses(n),this._elementInfo.delete(n),this._removeGlobalListeners(i))}focusVia(e,n,i){let a=be(e),o=this._document.activeElement;a===o?this._getClosestElementsInfo(a).forEach(([s,c])=>this._originChanged(s,n,c)):(this._setOrigin(n),typeof a.focus=="function"&&a.focus(i))}ngOnDestroy(){this._elementInfo.forEach((e,n)=>this.stopMonitoring(n))}_getWindow(){return this._document.defaultView||window}_getFocusOrigin(e){return this._origin?this._originFromTouchInteraction?this._shouldBeAttributedToTouch(e)?"touch":"program":this._origin:this._windowFocused&&this._lastFocusOrigin?this._lastFocusOrigin:e&&this._isLastInteractionFromInputLabel(e)?"mouse":"program"}_shouldBeAttributedToTouch(e){return this._detectionMode===Et.EVENTUAL||!!e?.contains(this._inputModalityDetector._mostRecentTarget)}_setClasses(e,n){e.classList.toggle("cdk-focused",!!n),e.classList.toggle("cdk-touch-focused",n==="touch"),e.classList.toggle("cdk-keyboard-focused",n==="keyboard"),e.classList.toggle("cdk-mouse-focused",n==="mouse"),e.classList.toggle("cdk-program-focused",n==="program")}_setOrigin(e,n=!1){this._ngZone.runOutsideAngular(()=>{if(this._origin=e,this._originFromTouchInteraction=e==="touch"&&n,this._detectionMode===Et.IMMEDIATE){clearTimeout(this._originTimeoutId);let i=this._originFromTouchInteraction?ms:1;this._originTimeoutId=setTimeout(()=>this._origin=null,i)}})}_onFocus(e,n){let i=this._elementInfo.get(n),a=X(e);!i||!i.checkChildren&&n!==a||this._originChanged(n,this._getFocusOrigin(a),i)}_onBlur(e,n){let i=this._elementInfo.get(n);!i||i.checkChildren&&e.relatedTarget instanceof Node&&n.contains(e.relatedTarget)||(this._setClasses(n),this._emitOrigin(i,null))}_emitOrigin(e,n){e.subject.observers.length&&this._ngZone.run(()=>e.subject.next(n))}_registerGlobalListeners(e){if(!this._platform.isBrowser)return;let n=e.rootNode,i=this._rootNodeFocusListenerCount.get(n)||0;i||this._ngZone.runOutsideAngular(()=>{n.addEventListener("focus",this._rootNodeFocusAndBlurListener,fn),n.addEventListener("blur",this._rootNodeFocusAndBlurListener,fn)}),this._rootNodeFocusListenerCount.set(n,i+1),++this._monitoredElementCount===1&&(this._ngZone.runOutsideAngular(()=>{this._getWindow().addEventListener("focus",this._windowFocusListener)}),this._inputModalityDetector.modalityDetected.pipe(Ft(this._stopInputModalityDetector)).subscribe(a=>{this._setOrigin(a,!0)}))}_removeGlobalListeners(e){let n=e.rootNode;if(this._rootNodeFocusListenerCount.has(n)){let i=this._rootNodeFocusListenerCount.get(n);i>1?this._rootNodeFocusListenerCount.set(n,i-1):(n.removeEventListener("focus",this._rootNodeFocusAndBlurListener,fn),n.removeEventListener("blur",this._rootNodeFocusAndBlurListener,fn),this._rootNodeFocusListenerCount.delete(n))}--this._monitoredElementCount||(this._getWindow().removeEventListener("focus",this._windowFocusListener),this._stopInputModalityDetector.next(),clearTimeout(this._windowFocusTimeoutId),clearTimeout(this._originTimeoutId))}_originChanged(e,n,i){this._setClasses(e,n),this._emitOrigin(i,n),this._lastFocusOrigin=n}_getClosestElementsInfo(e){let n=[];return this._elementInfo.forEach((i,a)=>{(a===e||i.checkChildren&&a.contains(e))&&n.push([a,i])}),n}_isLastInteractionFromInputLabel(e){let{_mostRecentTarget:n,mostRecentModality:i}=this._inputModalityDetector;if(i!=="mouse"||!n||n===e||e.nodeName!=="INPUT"&&e.nodeName!=="TEXTAREA"||e.disabled)return!1;let a=e.labels;if(a){for(let o=0;o<a.length;o++)if(a[o].contains(n))return!0}return!1}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})(),Vf=(()=>{class t{_elementRef=l(H);_focusMonitor=l(mn);_monitorSubscription;_focusOrigin=null;cdkFocusChange=new Rt;constructor(){}get focusOrigin(){return this._focusOrigin}ngAfterViewInit(){let e=this._elementRef.nativeElement;this._monitorSubscription=this._focusMonitor.monitor(e,e.nodeType===1&&e.hasAttribute("cdkMonitorSubtreeFocus")).subscribe(n=>{this._focusOrigin=n,this.cdkFocusChange.emit(n)})}ngOnDestroy(){this._focusMonitor.stopMonitoring(this._elementRef),this._monitorSubscription?.unsubscribe()}static \u0275fac=function(n){return new(n||t)};static \u0275dir=B({type:t,selectors:[["","cdkMonitorElementFocus",""],["","cdkMonitorSubtreeFocus",""]],outputs:{cdkFocusChange:"cdkFocusChange"},exportAs:["cdkMonitorFocus"]})}return t})();var hn=new WeakMap,ce=(()=>{class t{_appRef;_injector=l(U);_environmentInjector=l(Ze);load(e){let n=this._appRef=this._appRef||this._injector.get(Rn),i=hn.get(n);i||(i={loaders:new Set,refs:[]},hn.set(n,i),n.onDestroy(()=>{hn.get(n)?.refs.forEach(a=>a.destroy()),hn.delete(n)})),i.loaders.has(e)||(i.loaders.add(e),i.refs.push(Bi(e,{environmentInjector:this._environmentInjector})))}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();var gn=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275cmp=V({type:t,selectors:[["ng-component"]],exportAs:["cdkVisuallyHidden"],decls:0,vars:0,template:function(n,i){},styles:[`.cdk-visually-hidden {
  border: 0;
  clip: rect(0 0 0 0);
  height: 1px;
  margin: -1px;
  overflow: hidden;
  padding: 0;
  position: absolute;
  width: 1px;
  white-space: nowrap;
  outline: 0;
  -webkit-appearance: none;
  -moz-appearance: none;
  left: 0;
}
[dir=rtl] .cdk-visually-hidden {
  left: auto;
  right: 0;
}
`],encapsulation:2,changeDetection:0})}return t})(),pn;function Wf(){if(pn===void 0&&(pn=null,typeof window<"u")){let t=window;t.trustedTypes!==void 0&&(pn=t.trustedTypes.createPolicy("angular#components",{createHTML:r=>r}))}return pn}function Gf(t){return Wf()?.createHTML(t)||t}function gs(t,r,e){let n=e.sanitize(ne.HTML,r);t.innerHTML=Gf(n||"")}var Yf=(()=>{class t{create(e){return typeof MutationObserver>"u"?null:new MutationObserver(e)}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();var bs=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275mod=L({type:t});static \u0275inj=P({providers:[Yf]})}return t})();var _s=(()=>{class t{_platform=l(j);constructor(){}isDisabled(e){return e.hasAttribute("disabled")}isVisible(e){return Zf(e)&&getComputedStyle(e).visibility==="visible"}isTabbable(e){if(!this._platform.isBrowser)return!1;let n=Kf(rm(e));if(n&&(vs(n)===-1||!this.isVisible(n)))return!1;let i=e.nodeName.toLowerCase(),a=vs(e);return e.hasAttribute("contenteditable")?a!==-1:i==="iframe"||i==="object"||this._platform.WEBKIT&&this._platform.IOS&&!tm(e)?!1:i==="audio"?e.hasAttribute("controls")?a!==-1:!1:i==="video"?a===-1?!1:a!==null?!0:this._platform.FIREFOX||e.hasAttribute("controls"):e.tabIndex>=0}isFocusable(e,n){return nm(e)&&!this.isDisabled(e)&&(n?.ignoreVisibility||this.isVisible(e))}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();function Kf(t){try{return t.frameElement}catch{return null}}function Zf(t){return!!(t.offsetWidth||t.offsetHeight||typeof t.getClientRects=="function"&&t.getClientRects().length)}function Xf(t){let r=t.nodeName.toLowerCase();return r==="input"||r==="select"||r==="button"||r==="textarea"}function Jf(t){return Qf(t)&&t.type=="hidden"}function qf(t){return em(t)&&t.hasAttribute("href")}function Qf(t){return t.nodeName.toLowerCase()=="input"}function em(t){return t.nodeName.toLowerCase()=="a"}function Es(t){if(!t.hasAttribute("tabindex")||t.tabIndex===void 0)return!1;let r=t.getAttribute("tabindex");return!!(r&&!isNaN(parseInt(r,10)))}function vs(t){if(!Es(t))return null;let r=parseInt(t.getAttribute("tabindex")||"",10);return isNaN(r)?-1:r}function tm(t){let r=t.nodeName.toLowerCase(),e=r==="input"&&t.type;return e==="text"||e==="password"||r==="select"||r==="textarea"}function nm(t){return Jf(t)?!1:Xf(t)||qf(t)||t.hasAttribute("contenteditable")||Es(t)}function rm(t){return t.ownerDocument&&t.ownerDocument.defaultView||window}var bn=class{_element;_checker;_ngZone;_document;_injector;_startAnchor=null;_endAnchor=null;_hasAttached=!1;startAnchorListener=()=>this.focusLastTabbableElement();endAnchorListener=()=>this.focusFirstTabbableElement();get enabled(){return this._enabled}set enabled(r){this._enabled=r,this._startAnchor&&this._endAnchor&&(this._toggleAnchorTabIndex(r,this._startAnchor),this._toggleAnchorTabIndex(r,this._endAnchor))}_enabled=!0;constructor(r,e,n,i,a=!1,o){this._element=r,this._checker=e,this._ngZone=n,this._document=i,this._injector=o,a||this.attachAnchors()}destroy(){let r=this._startAnchor,e=this._endAnchor;r&&(r.removeEventListener("focus",this.startAnchorListener),r.remove()),e&&(e.removeEventListener("focus",this.endAnchorListener),e.remove()),this._startAnchor=this._endAnchor=null,this._hasAttached=!1}attachAnchors(){return this._hasAttached?!0:(this._ngZone.runOutsideAngular(()=>{this._startAnchor||(this._startAnchor=this._createAnchor(),this._startAnchor.addEventListener("focus",this.startAnchorListener)),this._endAnchor||(this._endAnchor=this._createAnchor(),this._endAnchor.addEventListener("focus",this.endAnchorListener))}),this._element.parentNode&&(this._element.parentNode.insertBefore(this._startAnchor,this._element),this._element.parentNode.insertBefore(this._endAnchor,this._element.nextSibling),this._hasAttached=!0),this._hasAttached)}focusInitialElementWhenReady(r){return new Promise(e=>{this._executeOnStable(()=>e(this.focusInitialElement(r)))})}focusFirstTabbableElementWhenReady(r){return new Promise(e=>{this._executeOnStable(()=>e(this.focusFirstTabbableElement(r)))})}focusLastTabbableElementWhenReady(r){return new Promise(e=>{this._executeOnStable(()=>e(this.focusLastTabbableElement(r)))})}_getRegionBoundary(r){let e=this._element.querySelectorAll(`[cdk-focus-region-${r}], [cdkFocusRegion${r}], [cdk-focus-${r}]`);return r=="start"?e.length?e[0]:this._getFirstTabbableElement(this._element):e.length?e[e.length-1]:this._getLastTabbableElement(this._element)}focusInitialElement(r){let e=this._element.querySelector("[cdk-focus-initial], [cdkFocusInitial]");if(e){if(!this._checker.isFocusable(e)){let n=this._getFirstTabbableElement(e);return n?.focus(r),!!n}return e.focus(r),!0}return this.focusFirstTabbableElement(r)}focusFirstTabbableElement(r){let e=this._getRegionBoundary("start");return e&&e.focus(r),!!e}focusLastTabbableElement(r){let e=this._getRegionBoundary("end");return e&&e.focus(r),!!e}hasAttached(){return this._hasAttached}_getFirstTabbableElement(r){if(this._checker.isFocusable(r)&&this._checker.isTabbable(r))return r;let e=r.children;for(let n=0;n<e.length;n++){let i=e[n].nodeType===this._document.ELEMENT_NODE?this._getFirstTabbableElement(e[n]):null;if(i)return i}return null}_getLastTabbableElement(r){if(this._checker.isFocusable(r)&&this._checker.isTabbable(r))return r;let e=r.children;for(let n=e.length-1;n>=0;n--){let i=e[n].nodeType===this._document.ELEMENT_NODE?this._getLastTabbableElement(e[n]):null;if(i)return i}return null}_createAnchor(){let r=this._document.createElement("div");return this._toggleAnchorTabIndex(this._enabled,r),r.classList.add("cdk-visually-hidden"),r.classList.add("cdk-focus-trap-anchor"),r.setAttribute("aria-hidden","true"),r}_toggleAnchorTabIndex(r,e){r?e.setAttribute("tabindex","0"):e.removeAttribute("tabindex")}toggleAnchors(r){this._startAnchor&&this._endAnchor&&(this._toggleAnchorTabIndex(r,this._startAnchor),this._toggleAnchorTabIndex(r,this._endAnchor))}_executeOnStable(r){this._injector?xi(r,{injector:this._injector}):setTimeout(r)}},ws=(()=>{class t{_checker=l(_s);_ngZone=l(O);_document=l(I);_injector=l(U);constructor(){l(ce).load(gn)}create(e,n=!1){return new bn(e,this._checker,this._ngZone,this._document,n,this._injector)}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})(),im=(()=>{class t{_elementRef=l(H);_focusTrapFactory=l(ws);focusTrap=void 0;_previouslyFocusedElement=null;get enabled(){return this.focusTrap?.enabled||!1}set enabled(e){this.focusTrap&&(this.focusTrap.enabled=e)}autoCapture=!1;constructor(){l(j).isBrowser&&(this.focusTrap=this._focusTrapFactory.create(this._elementRef.nativeElement,!0))}ngOnDestroy(){this.focusTrap?.destroy(),this._previouslyFocusedElement&&(this._previouslyFocusedElement.focus(),this._previouslyFocusedElement=null)}ngAfterContentInit(){this.focusTrap?.attachAnchors(),this.autoCapture&&this._captureFocus()}ngDoCheck(){this.focusTrap&&!this.focusTrap.hasAttached()&&this.focusTrap.attachAnchors()}ngOnChanges(e){let n=e.autoCapture;n&&!n.firstChange&&this.autoCapture&&this.focusTrap?.hasAttached()&&this._captureFocus()}_captureFocus(){this._previouslyFocusedElement=Hr(),this.focusTrap?.focusInitialElementWhenReady()}static \u0275fac=function(n){return new(n||t)};static \u0275dir=B({type:t,selectors:[["","cdkTrapFocus",""]],inputs:{enabled:[2,"cdkTrapFocus","enabled",W],autoCapture:[2,"cdkTrapFocusAutoCapture","autoCapture",W]},exportAs:["cdkTrapFocus"],features:[Te]})}return t})(),As=new C("liveAnnouncerElement",{providedIn:"root",factory:()=>null}),Cs=new C("LIVE_ANNOUNCER_DEFAULT_OPTIONS"),am=0,om=(()=>{class t{_ngZone=l(O);_defaultOptions=l(Cs,{optional:!0});_liveElement;_document=l(I);_sanitizer=l(dt);_previousTimeout;_currentPromise;_currentResolve;constructor(){let e=l(As,{optional:!0});this._liveElement=e||this._createLiveElement()}announce(e,...n){let i=this._defaultOptions,a,o;return n.length===1&&typeof n[0]=="number"?o=n[0]:[a,o]=n,this.clear(),clearTimeout(this._previousTimeout),a||(a=i&&i.politeness?i.politeness:"polite"),o==null&&i&&(o=i.duration),this._liveElement.setAttribute("aria-live",a),this._liveElement.id&&this._exposeAnnouncerToModals(this._liveElement.id),this._ngZone.runOutsideAngular(()=>(this._currentPromise||(this._currentPromise=new Promise(s=>this._currentResolve=s)),clearTimeout(this._previousTimeout),this._previousTimeout=setTimeout(()=>{!e||typeof e=="string"?this._liveElement.textContent=e:gs(this._liveElement,e,this._sanitizer),typeof o=="number"&&(this._previousTimeout=setTimeout(()=>this.clear(),o)),this._currentResolve?.(),this._currentPromise=this._currentResolve=void 0},100),this._currentPromise))}clear(){this._liveElement&&(this._liveElement.textContent="")}ngOnDestroy(){clearTimeout(this._previousTimeout),this._liveElement?.remove(),this._liveElement=null,this._currentResolve?.(),this._currentPromise=this._currentResolve=void 0}_createLiveElement(){let e="cdk-live-announcer-element",n=this._document.getElementsByClassName(e),i=this._document.createElement("div");for(let a=0;a<n.length;a++)n[a].remove();return i.classList.add(e),i.classList.add("cdk-visually-hidden"),i.setAttribute("aria-atomic","true"),i.setAttribute("aria-live","polite"),i.id=`cdk-live-announcer-${am++}`,this._document.body.appendChild(i),i}_exposeAnnouncerToModals(e){let n=this._document.querySelectorAll('body > .cdk-overlay-container [aria-modal="true"]');for(let i=0;i<n.length;i++){let a=n[i],o=a.getAttribute("aria-owns");o?o.indexOf(e)===-1&&a.setAttribute("aria-owns",o+" "+e):a.setAttribute("aria-owns",e)}}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();var ve=(function(t){return t[t.NONE=0]="NONE",t[t.BLACK_ON_WHITE=1]="BLACK_ON_WHITE",t[t.WHITE_ON_BLACK=2]="WHITE_ON_BLACK",t})(ve||{}),ys="cdk-high-contrast-black-on-white",Ds="cdk-high-contrast-white-on-black",Wr="cdk-high-contrast-active",Is=(()=>{class t{_platform=l(j);_hasCheckedHighContrastMode=!1;_document=l(I);_breakpointSubscription;constructor(){this._breakpointSubscription=l(Br).observe("(forced-colors: active)").subscribe(()=>{this._hasCheckedHighContrastMode&&(this._hasCheckedHighContrastMode=!1,this._applyBodyHighContrastModeCssClasses())})}getHighContrastMode(){if(!this._platform.isBrowser)return ve.NONE;let e=this._document.createElement("div");e.style.backgroundColor="rgb(1,2,3)",e.style.position="absolute",this._document.body.appendChild(e);let n=this._document.defaultView||window,i=n&&n.getComputedStyle?n.getComputedStyle(e):null,a=(i&&i.backgroundColor||"").replace(/ /g,"");switch(e.remove(),a){case"rgb(0,0,0)":case"rgb(45,50,54)":case"rgb(32,32,32)":return ve.WHITE_ON_BLACK;case"rgb(255,255,255)":case"rgb(255,250,239)":return ve.BLACK_ON_WHITE}return ve.NONE}ngOnDestroy(){this._breakpointSubscription.unsubscribe()}_applyBodyHighContrastModeCssClasses(){if(!this._hasCheckedHighContrastMode&&this._platform.isBrowser&&this._document.body){let e=this._document.body.classList;e.remove(Wr,ys,Ds),this._hasCheckedHighContrastMode=!0;let n=this.getHighContrastMode();n===ve.BLACK_ON_WHITE?e.add(Wr,ys):n===ve.WHITE_ON_BLACK&&e.add(Wr,Ds)}}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})(),sm=(()=>{class t{constructor(){l(Is)._applyBodyHighContrastModeCssClasses()}static \u0275fac=function(n){return new(n||t)};static \u0275mod=L({type:t});static \u0275inj=P({imports:[bs]})}return t})();var cm=200,vn=class{_letterKeyStream=new $;_items=[];_selectedItemIndex=-1;_pressedLetters=[];_skipPredicateFn;_selectedItem=new $;selectedItem=this._selectedItem;constructor(r,e){let n=typeof e?.debounceInterval=="number"?e.debounceInterval:cm;e?.skipPredicate&&(this._skipPredicateFn=e.skipPredicate),this.setItems(r),this._setupKeyHandler(n)}destroy(){this._pressedLetters=[],this._letterKeyStream.complete(),this._selectedItem.complete()}setCurrentSelectedItemIndex(r){this._selectedItemIndex=r}setItems(r){this._items=r}handleKey(r){let e=r.keyCode;r.key&&r.key.length===1?this._letterKeyStream.next(r.key.toLocaleUpperCase()):(e>=65&&e<=90||e>=48&&e<=57)&&this._letterKeyStream.next(String.fromCharCode(e))}isTyping(){return this._pressedLetters.length>0}reset(){this._pressedLetters=[]}_setupKeyHandler(r){this._letterKeyStream.pipe(fi(e=>this._pressedLetters.push(e)),Mt(r),St(()=>this._pressedLetters.length>0),J(()=>this._pressedLetters.join("").toLocaleUpperCase())).subscribe(e=>{for(let n=1;n<this._items.length+1;n++){let i=(this._selectedItemIndex+n)%this._items.length,a=this._items[i];if(!this._skipPredicateFn?.(a)&&a.getLabel?.().toLocaleUpperCase().trim().indexOf(e)===0){this._selectedItem.next(a);break}}this._pressedLetters=[]})}};var Ge=class{_items;_activeItemIndex=Xe(-1);_activeItem=Xe(null);_wrap=!1;_typeaheadSubscription=ni.EMPTY;_itemChangesSubscription;_vertical=!0;_horizontal=null;_allowedModifierKeys=[];_homeAndEnd=!1;_pageUpAndDown={enabled:!1,delta:10};_effectRef;_typeahead;_skipPredicateFn=r=>r.disabled;constructor(r,e){this._items=r,r instanceof Sn?this._itemChangesSubscription=r.changes.subscribe(n=>this._itemsChanged(n.toArray())):Pt(r)&&(this._effectRef=Ot(()=>this._itemsChanged(r()),{injector:e}))}tabOut=new $;change=new $;skipPredicate(r){return this._skipPredicateFn=r,this}withWrap(r=!0){return this._wrap=r,this}withVerticalOrientation(r=!0){return this._vertical=r,this}withHorizontalOrientation(r){return this._horizontal=r,this}withAllowedModifierKeys(r){return this._allowedModifierKeys=r,this}withTypeAhead(r=200){this._typeaheadSubscription.unsubscribe();let e=this._getItemsArray();return this._typeahead=new vn(e,{debounceInterval:typeof r=="number"?r:void 0,skipPredicate:n=>this._skipPredicateFn(n)}),this._typeaheadSubscription=this._typeahead.selectedItem.subscribe(n=>{this.setActiveItem(n)}),this}cancelTypeahead(){return this._typeahead?.reset(),this}withHomeAndEnd(r=!0){return this._homeAndEnd=r,this}withPageUpDown(r=!0,e=10){return this._pageUpAndDown={enabled:r,delta:e},this}setActiveItem(r){let e=this._activeItem();this.updateActiveItem(r),this._activeItem()!==e&&this.change.next(this._activeItemIndex())}onKeydown(r){let e=r.keyCode,i=["altKey","ctrlKey","metaKey","shiftKey"].every(a=>!r[a]||this._allowedModifierKeys.indexOf(a)>-1);switch(e){case 9:this.tabOut.next();return;case 40:if(this._vertical&&i){this.setNextItemActive();break}else return;case 38:if(this._vertical&&i){this.setPreviousItemActive();break}else return;case 39:if(this._horizontal&&i){this._horizontal==="rtl"?this.setPreviousItemActive():this.setNextItemActive();break}else return;case 37:if(this._horizontal&&i){this._horizontal==="rtl"?this.setNextItemActive():this.setPreviousItemActive();break}else return;case 36:if(this._homeAndEnd&&i){this.setFirstItemActive();break}else return;case 35:if(this._homeAndEnd&&i){this.setLastItemActive();break}else return;case 33:if(this._pageUpAndDown.enabled&&i){let a=this._activeItemIndex()-this._pageUpAndDown.delta;this._setActiveItemByIndex(a>0?a:0,1);break}else return;case 34:if(this._pageUpAndDown.enabled&&i){let a=this._activeItemIndex()+this._pageUpAndDown.delta,o=this._getItemsArray().length;this._setActiveItemByIndex(a<o?a:o-1,-1);break}else return;default:(i||cs(r,"shiftKey"))&&this._typeahead?.handleKey(r);return}this._typeahead?.reset(),r.preventDefault()}get activeItemIndex(){return this._activeItemIndex()}get activeItem(){return this._activeItem()}isTyping(){return!!this._typeahead&&this._typeahead.isTyping()}setFirstItemActive(){this._setActiveItemByIndex(0,1)}setLastItemActive(){this._setActiveItemByIndex(this._getItemsArray().length-1,-1)}setNextItemActive(){this._activeItemIndex()<0?this.setFirstItemActive():this._setActiveItemByDelta(1)}setPreviousItemActive(){this._activeItemIndex()<0&&this._wrap?this.setLastItemActive():this._setActiveItemByDelta(-1)}updateActiveItem(r){let e=this._getItemsArray(),n=typeof r=="number"?r:e.indexOf(r),i=e[n];this._activeItem.set(i??null),this._activeItemIndex.set(n),this._typeahead?.setCurrentSelectedItemIndex(n)}destroy(){this._typeaheadSubscription.unsubscribe(),this._itemChangesSubscription?.unsubscribe(),this._effectRef?.destroy(),this._typeahead?.destroy(),this.tabOut.complete(),this.change.complete()}_setActiveItemByDelta(r){this._wrap?this._setActiveInWrapMode(r):this._setActiveInDefaultMode(r)}_setActiveInWrapMode(r){let e=this._getItemsArray();for(let n=1;n<=e.length;n++){let i=(this._activeItemIndex()+r*n+e.length)%e.length,a=e[i];if(!this._skipPredicateFn(a)){this.setActiveItem(i);return}}}_setActiveInDefaultMode(r){this._setActiveItemByIndex(this._activeItemIndex()+r,r)}_setActiveItemByIndex(r,e){let n=this._getItemsArray();if(n[r]){for(;this._skipPredicateFn(n[r]);)if(r+=e,!n[r])return;this.setActiveItem(r)}}_getItemsArray(){return Pt(this._items)?this._items():this._items instanceof Sn?this._items.toArray():this._items}_itemsChanged(r){this._typeahead?.setItems(r);let e=this._activeItem();if(e){let n=r.indexOf(e);n>-1&&n!==this._activeItemIndex()&&(this._activeItemIndex.set(n),this._typeahead?.setCurrentSelectedItemIndex(n))}}};var Gr=class extends Ge{setActiveItem(r){this.activeItem&&this.activeItem.setInactiveStyles(),super.setActiveItem(r),this.activeItem&&this.activeItem.setActiveStyles()}};var Yr=class extends Ge{_origin="program";setFocusOrigin(r){return this._origin=r,this}setActiveItem(r){super.setActiveItem(r),this.activeItem&&this.activeItem.focus(this._origin)}};var Ss=" ";function um(t,r,e){let n=Dn(t,r);e=e.trim(),!n.some(i=>i.trim()===e)&&(n.push(e),t.setAttribute(r,n.join(Ss)))}function lm(t,r,e){let n=Dn(t,r);e=e.trim();let i=n.filter(a=>a!==e);i.length?t.setAttribute(r,i.join(Ss)):t.removeAttribute(r)}function Dn(t,r){return t.getAttribute(r)?.match(/\S+/g)??[]}var Ms="cdk-describedby-message",yn="cdk-describedby-host",Zr=0,py=(()=>{class t{_platform=l(j);_document=l(I);_messageRegistry=new Map;_messagesContainer=null;_id=`${Zr++}`;constructor(){l(ce).load(gn),this._id=l(ye)+"-"+Zr++}describe(e,n,i){if(!this._canBeDescribed(e,n))return;let a=Kr(n,i);typeof n!="string"?(Ts(n,this._id),this._messageRegistry.set(a,{messageElement:n,referenceCount:0})):this._messageRegistry.has(a)||this._createMessageElement(n,i),this._isElementDescribedByMessage(e,a)||this._addMessageReference(e,a)}removeDescription(e,n,i){if(!n||!this._isElementNode(e))return;let a=Kr(n,i);if(this._isElementDescribedByMessage(e,a)&&this._removeMessageReference(e,a),typeof n=="string"){let o=this._messageRegistry.get(a);o&&o.referenceCount===0&&this._deleteMessageElement(a)}this._messagesContainer?.childNodes.length===0&&(this._messagesContainer.remove(),this._messagesContainer=null)}ngOnDestroy(){let e=this._document.querySelectorAll(`[${yn}="${this._id}"]`);for(let n=0;n<e.length;n++)this._removeCdkDescribedByReferenceIds(e[n]),e[n].removeAttribute(yn);this._messagesContainer?.remove(),this._messagesContainer=null,this._messageRegistry.clear()}_createMessageElement(e,n){let i=this._document.createElement("div");Ts(i,this._id),i.textContent=e,n&&i.setAttribute("role",n),this._createMessagesContainer(),this._messagesContainer.appendChild(i),this._messageRegistry.set(Kr(e,n),{messageElement:i,referenceCount:0})}_deleteMessageElement(e){this._messageRegistry.get(e)?.messageElement?.remove(),this._messageRegistry.delete(e)}_createMessagesContainer(){if(this._messagesContainer)return;let e="cdk-describedby-message-container",n=this._document.querySelectorAll(`.${e}[platform="server"]`);for(let a=0;a<n.length;a++)n[a].remove();let i=this._document.createElement("div");i.style.visibility="hidden",i.classList.add(e),i.classList.add("cdk-visually-hidden"),this._platform.isBrowser||i.setAttribute("platform","server"),this._document.body.appendChild(i),this._messagesContainer=i}_removeCdkDescribedByReferenceIds(e){let n=Dn(e,"aria-describedby").filter(i=>i.indexOf(Ms)!=0);e.setAttribute("aria-describedby",n.join(" "))}_addMessageReference(e,n){let i=this._messageRegistry.get(n);um(e,"aria-describedby",i.messageElement.id),e.setAttribute(yn,this._id),i.referenceCount++}_removeMessageReference(e,n){let i=this._messageRegistry.get(n);i.referenceCount--,lm(e,"aria-describedby",i.messageElement.id),e.removeAttribute(yn)}_isElementDescribedByMessage(e,n){let i=Dn(e,"aria-describedby"),a=this._messageRegistry.get(n),o=a&&a.messageElement.id;return!!o&&i.indexOf(o)!=-1}_canBeDescribed(e,n){if(!this._isElementNode(e))return!1;if(n&&typeof n=="object")return!0;let i=n==null?"":`${n}`.trim(),a=e.getAttribute("aria-label");return i?!a||a.trim()!==i:!1}_isElementNode(e){return e.nodeType===this._document.ELEMENT_NODE}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();function Kr(t,r){return typeof t=="string"?`${r||""}/${t}`:t}function Ts(t,r){t.id||(t.id=`${Ms}-${r}-${Zr++}`)}function wy(t){return t==null?"":typeof t=="string"?t:`${t}px`}function Cy(t){return t!=null&&`${t}`!="false"}function Iy(t,r=/\s+/){let e=[];if(t!=null){let n=Array.isArray(t)?t:`${t}`.split(r);for(let i of n){let a=`${i}`.trim();a&&e.push(a)}}return e}var dm=new C("cdk-dir-doc",{providedIn:"root",factory:()=>l(I)}),fm=/^(ar|ckb|dv|he|iw|fa|nqo|ps|sd|ug|ur|yi|.*[-_](Adlm|Arab|Hebr|Nkoo|Rohg|Thaa))(?!.*[-_](Latn|Cyrl)($|-|_))($|-|_)/i;function xs(t){let r=t?.toLowerCase()||"";return r==="auto"&&typeof navigator<"u"&&navigator?.language?fm.test(navigator.language)?"rtl":"ltr":r==="rtl"?"rtl":"ltr"}var mm=(()=>{class t{get value(){return this.valueSignal()}valueSignal=Xe("ltr");change=new Rt;constructor(){let e=l(dm,{optional:!0});if(e){let n=e.body?e.body.dir:null,i=e.documentElement?e.documentElement.dir:null;this.valueSignal.set(xs(n||i||"ltr"))}}ngOnDestroy(){this.change.complete()}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();var _n=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275mod=L({type:t});static \u0275inj=P({})}return t})();var wt=(function(t){return t[t.NORMAL=0]="NORMAL",t[t.NEGATED=1]="NEGATED",t[t.INVERTED=2]="INVERTED",t})(wt||{}),En,Ie;function Ny(){if(Ie==null){if(typeof document!="object"||!document||typeof Element!="function"||!Element)return Ie=!1,Ie;if(document.documentElement?.style&&"scrollBehavior"in document.documentElement.style)Ie=!0;else{let t=Element.prototype.scrollTo;t?Ie=!/\{\s*\[native code\]\s*\}/.test(t.toString()):Ie=!1}}return Ie}function Py(){if(typeof document!="object"||!document)return wt.NORMAL;if(En==null){let t=document.createElement("div"),r=t.style;t.dir="rtl",r.width="1px",r.overflow="auto",r.visibility="hidden",r.pointerEvents="none",r.position="absolute";let e=document.createElement("div"),n=e.style;n.width="2px",n.height="1px",t.appendChild(e),document.body.appendChild(t),En=wt.NORMAL,t.scrollLeft===0&&(t.scrollLeft=1,En=t.scrollLeft===0?wt.NEGATED:wt.INVERTED),t.remove()}return En}function By(){return typeof __karma__<"u"&&!!__karma__||typeof jasmine<"u"&&!!jasmine||typeof jest<"u"&&!!jest||typeof Mocha<"u"&&!!Mocha}var Ye,Fs=["color","button","checkbox","date","datetime-local","email","file","hidden","image","month","number","password","radio","range","reset","search","submit","tel","text","time","url","week"];function Uy(){if(Ye)return Ye;if(typeof document!="object"||!document)return Ye=new Set(Fs),Ye;let t=document.createElement("input");return Ye=new Set(Fs.filter(r=>(t.setAttribute("type",r),t.type===r))),Ye}var Y=(function(t){return t[t.FADING_IN=0]="FADING_IN",t[t.VISIBLE=1]="VISIBLE",t[t.FADING_OUT=2]="FADING_OUT",t[t.HIDDEN=3]="HIDDEN",t})(Y||{}),Xr=class{_renderer;element;config;_animationForciblyDisabledThroughCss;state=Y.HIDDEN;constructor(r,e,n,i=!1){this._renderer=r,this.element=e,this.config=n,this._animationForciblyDisabledThroughCss=i}fadeOut(){this._renderer.fadeOutRipple(this)}},Rs=We({passive:!0,capture:!0}),Jr=class{_events=new Map;addHandler(r,e,n,i){let a=this._events.get(e);if(a){let o=a.get(n);o?o.add(i):a.set(n,new Set([i]))}else this._events.set(e,new Map([[n,new Set([i])]])),r.runOutsideAngular(()=>{document.addEventListener(e,this._delegateEventHandler,Rs)})}removeHandler(r,e,n){let i=this._events.get(r);if(!i)return;let a=i.get(e);a&&(a.delete(n),a.size===0&&i.delete(e),i.size===0&&(this._events.delete(r),document.removeEventListener(r,this._delegateEventHandler,Rs)))}_delegateEventHandler=r=>{let e=X(r);e&&this._events.get(r.type)?.forEach((n,i)=>{(i===e||i.contains(e))&&n.forEach(a=>a.handleEvent(r))})}},At={enterDuration:225,exitDuration:150},hm=800,Os=We({passive:!0,capture:!0}),ks=["mousedown","touchstart"],Ns=["mouseup","mouseleave","touchend","touchcancel"],pm=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275cmp=V({type:t,selectors:[["ng-component"]],hostAttrs:["mat-ripple-style-loader",""],decls:0,vars:0,template:function(n,i){},styles:[`.mat-ripple {
  overflow: hidden;
  position: relative;
}
.mat-ripple:not(:empty) {
  transform: translateZ(0);
}

.mat-ripple.mat-ripple-unbounded {
  overflow: visible;
}

.mat-ripple-element {
  position: absolute;
  border-radius: 50%;
  pointer-events: none;
  transition: opacity, transform 0ms cubic-bezier(0, 0, 0.2, 1);
  transform: scale3d(0, 0, 0);
  background-color: var(--mat-ripple-color, color-mix(in srgb, var(--mat-sys-on-surface) 10%, transparent));
}
@media (forced-colors: active) {
  .mat-ripple-element {
    display: none;
  }
}
.cdk-drag-preview .mat-ripple-element, .cdk-drag-placeholder .mat-ripple-element {
  display: none;
}
`],encapsulation:2,changeDetection:0})}return t})(),Ct=class t{_target;_ngZone;_platform;_containerElement;_triggerElement=null;_isPointerDown=!1;_activeRipples=new Map;_mostRecentTransientRipple=null;_lastTouchStartEvent;_pointerUpEventsRegistered=!1;_containerRect=null;static _eventManager=new Jr;constructor(r,e,n,i,a){this._target=r,this._ngZone=e,this._platform=i,i.isBrowser&&(this._containerElement=be(n)),a&&a.get(ce).load(pm)}fadeInRipple(r,e,n={}){let i=this._containerRect=this._containerRect||this._containerElement.getBoundingClientRect(),a=R(R({},At),n.animation);n.centered&&(r=i.left+i.width/2,e=i.top+i.height/2);let o=n.radius||gm(r,e,i),s=r-i.left,c=e-i.top,u=a.enterDuration,f=document.createElement("div");f.classList.add("mat-ripple-element"),f.style.left=`${s-o}px`,f.style.top=`${c-o}px`,f.style.height=`${o*2}px`,f.style.width=`${o*2}px`,n.color!=null&&(f.style.backgroundColor=n.color),f.style.transitionDuration=`${u}ms`,this._containerElement.appendChild(f);let d=window.getComputedStyle(f),p=d.transitionProperty,h=d.transitionDuration,E=p==="none"||h==="0s"||h==="0s, 0s"||i.width===0&&i.height===0,_=new Xr(this,f,n,E);f.style.transform="scale3d(1, 1, 1)",_.state=Y.FADING_IN,n.persistent||(this._mostRecentTransientRipple=_);let v=null;return!E&&(u||a.exitDuration)&&this._ngZone.runOutsideAngular(()=>{let y=()=>{v&&(v.fallbackTimer=null),clearTimeout(x),this._finishRippleTransition(_)},A=()=>this._destroyRipple(_),x=setTimeout(A,u+100);f.addEventListener("transitionend",y),f.addEventListener("transitioncancel",A),v={onTransitionEnd:y,onTransitionCancel:A,fallbackTimer:x}}),this._activeRipples.set(_,v),(E||!u)&&this._finishRippleTransition(_),_}fadeOutRipple(r){if(r.state===Y.FADING_OUT||r.state===Y.HIDDEN)return;let e=r.element,n=R(R({},At),r.config.animation);e.style.transitionDuration=`${n.exitDuration}ms`,e.style.opacity="0",r.state=Y.FADING_OUT,(r._animationForciblyDisabledThroughCss||!n.exitDuration)&&this._finishRippleTransition(r)}fadeOutAll(){this._getActiveRipples().forEach(r=>r.fadeOut())}fadeOutAllNonPersistent(){this._getActiveRipples().forEach(r=>{r.config.persistent||r.fadeOut()})}setupTriggerEvents(r){let e=be(r);!this._platform.isBrowser||!e||e===this._triggerElement||(this._removeTriggerEvents(),this._triggerElement=e,ks.forEach(n=>{t._eventManager.addHandler(this._ngZone,n,e,this)}))}handleEvent(r){r.type==="mousedown"?this._onMousedown(r):r.type==="touchstart"?this._onTouchStart(r):this._onPointerUp(),this._pointerUpEventsRegistered||(this._ngZone.runOutsideAngular(()=>{Ns.forEach(e=>{this._triggerElement.addEventListener(e,this,Os)})}),this._pointerUpEventsRegistered=!0)}_finishRippleTransition(r){r.state===Y.FADING_IN?this._startFadeOutTransition(r):r.state===Y.FADING_OUT&&this._destroyRipple(r)}_startFadeOutTransition(r){let e=r===this._mostRecentTransientRipple,{persistent:n}=r.config;r.state=Y.VISIBLE,!n&&(!e||!this._isPointerDown)&&r.fadeOut()}_destroyRipple(r){let e=this._activeRipples.get(r)??null;this._activeRipples.delete(r),this._activeRipples.size||(this._containerRect=null),r===this._mostRecentTransientRipple&&(this._mostRecentTransientRipple=null),r.state=Y.HIDDEN,e!==null&&(r.element.removeEventListener("transitionend",e.onTransitionEnd),r.element.removeEventListener("transitioncancel",e.onTransitionCancel),e.fallbackTimer!==null&&clearTimeout(e.fallbackTimer)),r.element.remove()}_onMousedown(r){let e=yt(r),n=this._lastTouchStartEvent&&Date.now()<this._lastTouchStartEvent+hm;!this._target.rippleDisabled&&!e&&!n&&(this._isPointerDown=!0,this.fadeInRipple(r.clientX,r.clientY,this._target.rippleConfig))}_onTouchStart(r){if(!this._target.rippleDisabled&&!Dt(r)){this._lastTouchStartEvent=Date.now(),this._isPointerDown=!0;let e=r.changedTouches;if(e)for(let n=0;n<e.length;n++)this.fadeInRipple(e[n].clientX,e[n].clientY,this._target.rippleConfig)}}_onPointerUp(){this._isPointerDown&&(this._isPointerDown=!1,this._getActiveRipples().forEach(r=>{let e=r.state===Y.VISIBLE||r.config.terminateOnPointerUp&&r.state===Y.FADING_IN;!r.config.persistent&&e&&r.fadeOut()}))}_getActiveRipples(){return Array.from(this._activeRipples.keys())}_removeTriggerEvents(){let r=this._triggerElement;r&&(ks.forEach(e=>t._eventManager.removeHandler(e,r,this)),this._pointerUpEventsRegistered&&(Ns.forEach(e=>r.removeEventListener(e,this,Os)),this._pointerUpEventsRegistered=!1))}};function gm(t,r,e){let n=Math.max(Math.abs(t-e.left),Math.abs(t-e.right)),i=Math.max(Math.abs(r-e.top),Math.abs(r-e.bottom));return Math.sqrt(n*n+i*i)}var qr=new C("mat-ripple-global-options"),n0=(()=>{class t{_elementRef=l(H);_animationsDisabled=Ve();color;unbounded=!1;centered=!1;radius=0;animation;get disabled(){return this._disabled}set disabled(e){e&&this.fadeOutAllNonPersistent(),this._disabled=e,this._setupTriggerEventsIfEnabled()}_disabled=!1;get trigger(){return this._trigger||this._elementRef.nativeElement}set trigger(e){this._trigger=e,this._setupTriggerEventsIfEnabled()}_trigger;_rippleRenderer;_globalOptions;_isInitialized=!1;constructor(){let e=l(O),n=l(j),i=l(qr,{optional:!0}),a=l(U);this._globalOptions=i||{},this._rippleRenderer=new Ct(this,e,this._elementRef,n,a)}ngOnInit(){this._isInitialized=!0,this._setupTriggerEventsIfEnabled()}ngOnDestroy(){this._rippleRenderer._removeTriggerEvents()}fadeOutAll(){this._rippleRenderer.fadeOutAll()}fadeOutAllNonPersistent(){this._rippleRenderer.fadeOutAllNonPersistent()}get rippleConfig(){return{centered:this.centered,radius:this.radius,color:this.color,animation:R(R(R({},this._globalOptions.animation),this._animationsDisabled?{enterDuration:0,exitDuration:0}:{}),this.animation),terminateOnPointerUp:this._globalOptions.terminateOnPointerUp}}get rippleDisabled(){return this.disabled||!!this._globalOptions.disabled}_setupTriggerEventsIfEnabled(){!this.disabled&&this._isInitialized&&this._rippleRenderer.setupTriggerEvents(this.trigger)}launch(e,n=0,i){return typeof e=="number"?this._rippleRenderer.fadeInRipple(e,n,R(R({},this.rippleConfig),i)):this._rippleRenderer.fadeInRipple(0,0,R(R({},this.rippleConfig),e))}static \u0275fac=function(n){return new(n||t)};static \u0275dir=B({type:t,selectors:[["","mat-ripple",""],["","matRipple",""]],hostAttrs:[1,"mat-ripple"],hostVars:2,hostBindings:function(n,i){n&2&&ke("mat-ripple-unbounded",i.unbounded)},inputs:{color:[0,"matRippleColor","color"],unbounded:[0,"matRippleUnbounded","unbounded"],centered:[0,"matRippleCentered","centered"],radius:[0,"matRippleRadius","radius"],animation:[0,"matRippleAnimation","animation"],disabled:[0,"matRippleDisabled","disabled"],trigger:[0,"matRippleTrigger","trigger"]},exportAs:["matRipple"]})}return t})();var bm={capture:!0},vm=["focus","mousedown","mouseenter","touchstart"],Qr="mat-ripple-loader-uninitialized",ei="mat-ripple-loader-class-name",Ps="mat-ripple-loader-centered",wn="mat-ripple-loader-disabled",Ls=(()=>{class t{_document=l(I);_animationsDisabled=Ve();_globalRippleOptions=l(qr,{optional:!0});_platform=l(j);_ngZone=l(O);_injector=l(U);_eventCleanups;_hosts=new Map;constructor(){let e=l(Fe).createRenderer(null,null);this._eventCleanups=this._ngZone.runOutsideAngular(()=>vm.map(n=>e.listen(this._document,n,this._onInteraction,bm)))}ngOnDestroy(){let e=this._hosts.keys();for(let n of e)this.destroyRipple(n);this._eventCleanups.forEach(n=>n())}configureRipple(e,n){e.setAttribute(Qr,this._globalRippleOptions?.namespace??""),(n.className||!e.hasAttribute(ei))&&e.setAttribute(ei,n.className||""),n.centered&&e.setAttribute(Ps,""),n.disabled&&e.setAttribute(wn,"")}setDisabled(e,n){let i=this._hosts.get(e);i?(i.target.rippleDisabled=n,!n&&!i.hasSetUpEvents&&(i.hasSetUpEvents=!0,i.renderer.setupTriggerEvents(e))):n?e.setAttribute(wn,""):e.removeAttribute(wn)}_onInteraction=e=>{let n=X(e);if(n instanceof HTMLElement){let i=n.closest(`[${Qr}="${this._globalRippleOptions?.namespace??""}"]`);i&&this._createRipple(i)}};_createRipple(e){if(!this._document||this._hosts.has(e))return;e.querySelector(".mat-ripple")?.remove();let n=this._document.createElement("span");n.classList.add("mat-ripple",e.getAttribute(ei)),e.append(n);let i=this._globalRippleOptions,a=this._animationsDisabled?0:i?.animation?.enterDuration??At.enterDuration,o=this._animationsDisabled?0:i?.animation?.exitDuration??At.exitDuration,s={rippleDisabled:this._animationsDisabled||i?.disabled||e.hasAttribute(wn),rippleConfig:{centered:e.hasAttribute(Ps),terminateOnPointerUp:i?.terminateOnPointerUp,animation:{enterDuration:a,exitDuration:o}}},c=new Ct(s,this._ngZone,n,this._platform,this._injector),u=!s.rippleDisabled;u&&c.setupTriggerEvents(e),this._hosts.set(e,{target:s,renderer:c,hasSetUpEvents:u}),e.removeAttribute(Qr)}destroyRipple(e){let n=this._hosts.get(e);n&&(n.renderer._removeTriggerEvents(),this._hosts.delete(e))}static \u0275fac=function(n){return new(n||t)};static \u0275prov=g({token:t,factory:t.\u0275fac,providedIn:"root"})}return t})();var Bs=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275cmp=V({type:t,selectors:[["structural-styles"]],decls:0,vars:0,template:function(n,i){},styles:[`.mat-focus-indicator {
  position: relative;
}
.mat-focus-indicator::before {
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  position: absolute;
  box-sizing: border-box;
  pointer-events: none;
  display: var(--mat-focus-indicator-display, none);
  border-width: var(--mat-focus-indicator-border-width, 3px);
  border-style: var(--mat-focus-indicator-border-style, solid);
  border-color: var(--mat-focus-indicator-border-color, transparent);
  border-radius: var(--mat-focus-indicator-border-radius, 4px);
}
.mat-focus-indicator:focus-visible::before {
  content: "";
}

@media (forced-colors: active) {
  html {
    --mat-focus-indicator-display: block;
  }
}
`],encapsulation:2,changeDetection:0})}return t})();var ym=["mat-icon-button",""],Dm=["*"],_m=new C("MAT_BUTTON_CONFIG");function js(t){return t==null?void 0:nt(t)}var ti=(()=>{class t{_elementRef=l(H);_ngZone=l(O);_animationsDisabled=Ve();_config=l(_m,{optional:!0});_focusMonitor=l(mn);_cleanupClick;_renderer=l(qe);_rippleLoader=l(Ls);_isAnchor;_isFab=!1;color;get disableRipple(){return this._disableRipple}set disableRipple(e){this._disableRipple=e,this._updateRippleDisabled()}_disableRipple=!1;get disabled(){return this._disabled}set disabled(e){this._disabled=e,this._updateRippleDisabled()}_disabled=!1;ariaDisabled;disabledInteractive;tabIndex;set _tabindex(e){this.tabIndex=e}constructor(){l(ce).load(Bs);let e=this._elementRef.nativeElement;this._isAnchor=e.tagName==="A",this.disabledInteractive=this._config?.disabledInteractive??!1,this.color=this._config?.color??null,this._rippleLoader?.configureRipple(e,{className:"mat-mdc-button-ripple"})}ngAfterViewInit(){this._focusMonitor.monitor(this._elementRef,!0),this._isAnchor&&this._setupAsAnchor()}ngOnDestroy(){this._cleanupClick?.(),this._focusMonitor.stopMonitoring(this._elementRef),this._rippleLoader?.destroyRipple(this._elementRef.nativeElement)}focus(e="program",n){e?this._focusMonitor.focusVia(this._elementRef.nativeElement,e,n):this._elementRef.nativeElement.focus(n)}_getAriaDisabled(){return this.ariaDisabled!=null?this.ariaDisabled:this._isAnchor?this.disabled||null:this.disabled&&this.disabledInteractive?!0:null}_getDisabledAttribute(){return this.disabledInteractive||!this.disabled?null:!0}_updateRippleDisabled(){this._rippleLoader?.setDisabled(this._elementRef.nativeElement,this.disableRipple||this.disabled)}_getTabIndex(){return this._isAnchor?this.disabled&&!this.disabledInteractive?-1:this.tabIndex:this.tabIndex}_setupAsAnchor(){this._cleanupClick=this._ngZone.runOutsideAngular(()=>this._renderer.listen(this._elementRef.nativeElement,"click",e=>{this.disabled&&(e.preventDefault(),e.stopImmediatePropagation())}))}static \u0275fac=function(n){return new(n||t)};static \u0275dir=B({type:t,hostAttrs:[1,"mat-mdc-button-base"],hostVars:13,hostBindings:function(n,i){n&2&&(et("disabled",i._getDisabledAttribute())("aria-disabled",i._getAriaDisabled())("tabindex",i._getTabIndex()),Lt(i.color?"mat-"+i.color:""),ke("mat-mdc-button-disabled",i.disabled)("mat-mdc-button-disabled-interactive",i.disabledInteractive)("mat-unthemed",!i.color)("_mat-animation-noopable",i._animationsDisabled))},inputs:{color:"color",disableRipple:[2,"disableRipple","disableRipple",W],disabled:[2,"disabled","disabled",W],ariaDisabled:[2,"aria-disabled","ariaDisabled",W],disabledInteractive:[2,"disabledInteractive","disabledInteractive",W],tabIndex:[2,"tabIndex","tabIndex",js],_tabindex:[2,"tabindex","_tabindex",js]}})}return t})(),Em=(()=>{class t extends ti{constructor(){super(),this._rippleLoader.configureRipple(this._elementRef.nativeElement,{centered:!0})}static \u0275fac=function(n){return new(n||t)};static \u0275cmp=V({type:t,selectors:[["button","mat-icon-button",""],["a","mat-icon-button",""],["button","matIconButton",""],["a","matIconButton",""]],hostAttrs:[1,"mdc-icon-button","mat-mdc-icon-button"],exportAs:["matButton","matAnchor"],features:[Qe],attrs:ym,ngContentSelectors:Dm,decls:4,vars:0,consts:[[1,"mat-mdc-button-persistent-ripple","mdc-icon-button__ripple"],[1,"mat-focus-indicator"],[1,"mat-mdc-button-touch-target"]],template:function(n,i){n&1&&(Oe(),Re(0,"span",0),le(1),Re(2,"span",1)(3,"span",2))},styles:[`.mat-mdc-icon-button {
  -webkit-user-select: none;
  user-select: none;
  display: inline-block;
  position: relative;
  box-sizing: border-box;
  border: none;
  outline: none;
  background-color: transparent;
  fill: currentColor;
  text-decoration: none;
  cursor: pointer;
  z-index: 0;
  overflow: visible;
  border-radius: var(--mat-icon-button-container-shape, var(--mat-sys-corner-full, 50%));
  flex-shrink: 0;
  text-align: center;
  width: var(--mat-icon-button-state-layer-size, 40px);
  height: var(--mat-icon-button-state-layer-size, 40px);
  padding: calc(calc(var(--mat-icon-button-state-layer-size, 40px) - var(--mat-icon-button-icon-size, 24px)) / 2);
  font-size: var(--mat-icon-button-icon-size, 24px);
  color: var(--mat-icon-button-icon-color, var(--mat-sys-on-surface-variant));
  -webkit-tap-highlight-color: transparent;
}
.mat-mdc-icon-button .mat-mdc-button-ripple,
.mat-mdc-icon-button .mat-mdc-button-persistent-ripple,
.mat-mdc-icon-button .mat-mdc-button-persistent-ripple::before {
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  position: absolute;
  pointer-events: none;
  border-radius: inherit;
}
.mat-mdc-icon-button .mat-mdc-button-ripple {
  overflow: hidden;
}
.mat-mdc-icon-button .mat-mdc-button-persistent-ripple::before {
  content: "";
  opacity: 0;
}
.mat-mdc-icon-button .mdc-button__label,
.mat-mdc-icon-button .mat-icon {
  z-index: 1;
  position: relative;
}
.mat-mdc-icon-button .mat-focus-indicator {
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  position: absolute;
  border-radius: inherit;
}
.mat-mdc-icon-button:focus-visible > .mat-focus-indicator::before {
  content: "";
  border-radius: inherit;
}
.mat-mdc-icon-button .mat-ripple-element {
  background-color: var(--mat-icon-button-ripple-color, color-mix(in srgb, var(--mat-sys-on-surface-variant) calc(var(--mat-sys-pressed-state-layer-opacity) * 100%), transparent));
}
.mat-mdc-icon-button .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-icon-button-state-layer-color, var(--mat-sys-on-surface-variant));
}
.mat-mdc-icon-button.mat-mdc-button-disabled .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-icon-button-disabled-state-layer-color, var(--mat-sys-on-surface-variant));
}
.mat-mdc-icon-button:hover > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-icon-button-hover-state-layer-opacity, var(--mat-sys-hover-state-layer-opacity));
}
.mat-mdc-icon-button.cdk-program-focused > .mat-mdc-button-persistent-ripple::before, .mat-mdc-icon-button.cdk-keyboard-focused > .mat-mdc-button-persistent-ripple::before, .mat-mdc-icon-button.mat-mdc-button-disabled-interactive:focus > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-icon-button-focus-state-layer-opacity, var(--mat-sys-focus-state-layer-opacity));
}
.mat-mdc-icon-button:active > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-icon-button-pressed-state-layer-opacity, var(--mat-sys-pressed-state-layer-opacity));
}
.mat-mdc-icon-button .mat-mdc-button-touch-target {
  position: absolute;
  top: 50%;
  height: var(--mat-icon-button-touch-target-size, 48px);
  display: var(--mat-icon-button-touch-target-display, block);
  left: 50%;
  width: var(--mat-icon-button-touch-target-size, 48px);
  transform: translate(-50%, -50%);
}
.mat-mdc-icon-button._mat-animation-noopable {
  transition: none !important;
  animation: none !important;
}
.mat-mdc-icon-button[disabled], .mat-mdc-icon-button.mat-mdc-button-disabled {
  cursor: default;
  pointer-events: none;
  color: var(--mat-icon-button-disabled-icon-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
}
.mat-mdc-icon-button.mat-mdc-button-disabled-interactive {
  pointer-events: auto;
}
.mat-mdc-icon-button img,
.mat-mdc-icon-button svg {
  width: var(--mat-icon-button-icon-size, 24px);
  height: var(--mat-icon-button-icon-size, 24px);
  vertical-align: baseline;
}
.mat-mdc-icon-button .mat-mdc-button-persistent-ripple {
  border-radius: var(--mat-icon-button-container-shape, var(--mat-sys-corner-full, 50%));
}
.mat-mdc-icon-button[hidden] {
  display: none;
}
.mat-mdc-icon-button.mat-unthemed:not(.mdc-ripple-upgraded):focus::before, .mat-mdc-icon-button.mat-primary:not(.mdc-ripple-upgraded):focus::before, .mat-mdc-icon-button.mat-accent:not(.mdc-ripple-upgraded):focus::before, .mat-mdc-icon-button.mat-warn:not(.mdc-ripple-upgraded):focus::before {
  background: transparent;
  opacity: 1;
}
`,`@media (forced-colors: active) {
  .mat-mdc-button:not(.mdc-button--outlined),
  .mat-mdc-unelevated-button:not(.mdc-button--outlined),
  .mat-mdc-raised-button:not(.mdc-button--outlined),
  .mat-mdc-outlined-button:not(.mdc-button--outlined),
  .mat-mdc-button-base.mat-tonal-button,
  .mat-mdc-icon-button.mat-mdc-icon-button,
  .mat-mdc-outlined-button .mdc-button__ripple {
    outline: solid 1px;
  }
}
`],encapsulation:2,changeDetection:0})}return t})();var Us=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275mod=L({type:t});static \u0275inj=P({imports:[_n]})}return t})();var wm=["matButton",""],Am=[[["",8,"material-icons",3,"iconPositionEnd",""],["mat-icon",3,"iconPositionEnd",""],["","matButtonIcon","",3,"iconPositionEnd",""]],"*",[["","iconPositionEnd","",8,"material-icons"],["mat-icon","iconPositionEnd",""],["","matButtonIcon","","iconPositionEnd",""]]],Cm=[".material-icons:not([iconPositionEnd]), mat-icon:not([iconPositionEnd]), [matButtonIcon]:not([iconPositionEnd])","*",".material-icons[iconPositionEnd], mat-icon[iconPositionEnd], [matButtonIcon][iconPositionEnd]"];var zs=new Map([["text",["mat-mdc-button"]],["filled",["mdc-button--unelevated","mat-mdc-unelevated-button"]],["elevated",["mdc-button--raised","mat-mdc-raised-button"]],["outlined",["mdc-button--outlined","mat-mdc-outlined-button"]],["tonal",["mat-tonal-button"]]]),M0=(()=>{class t extends ti{get appearance(){return this._appearance}set appearance(e){this.setAppearance(e||this._config?.defaultAppearance||"text")}_appearance=null;constructor(){super();let e=Im(this._elementRef.nativeElement);e&&this.setAppearance(e)}setAppearance(e){if(e===this._appearance)return;let n=this._elementRef.nativeElement.classList,i=this._appearance?zs.get(this._appearance):null,a=zs.get(e);i&&n.remove(...i),n.add(...a),this._appearance=e}static \u0275fac=function(n){return new(n||t)};static \u0275cmp=V({type:t,selectors:[["button","matButton",""],["a","matButton",""],["button","mat-button",""],["button","mat-raised-button",""],["button","mat-flat-button",""],["button","mat-stroked-button",""],["a","mat-button",""],["a","mat-raised-button",""],["a","mat-flat-button",""],["a","mat-stroked-button",""]],hostAttrs:[1,"mdc-button"],inputs:{appearance:[0,"matButton","appearance"]},exportAs:["matButton","matAnchor"],features:[Qe],attrs:wm,ngContentSelectors:Cm,decls:7,vars:4,consts:[[1,"mat-mdc-button-persistent-ripple"],[1,"mdc-button__label"],[1,"mat-focus-indicator"],[1,"mat-mdc-button-touch-target"]],template:function(n,i){n&1&&(Oe(Am),Re(0,"span",0),le(1),Ri(2,"span",1),le(3,1),Oi(),le(4,2),Re(5,"span",2)(6,"span",3)),n&2&&ke("mdc-button__ripple",!i._isFab)("mdc-fab__ripple",i._isFab)},styles:[`.mat-mdc-button-base {
  text-decoration: none;
}
.mat-mdc-button-base .mat-icon {
  min-height: fit-content;
  flex-shrink: 0;
}
@media (hover: none) {
  .mat-mdc-button-base:hover > span.mat-mdc-button-persistent-ripple::before {
    opacity: 0;
  }
}

.mdc-button {
  -webkit-user-select: none;
  user-select: none;
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  min-width: 64px;
  border: none;
  outline: none;
  line-height: inherit;
  -webkit-appearance: none;
  overflow: visible;
  vertical-align: middle;
  background: transparent;
  padding: 0 8px;
}
.mdc-button::-moz-focus-inner {
  padding: 0;
  border: 0;
}
.mdc-button:active {
  outline: none;
}
.mdc-button:hover {
  cursor: pointer;
}
.mdc-button:disabled {
  cursor: default;
  pointer-events: none;
}
.mdc-button[hidden] {
  display: none;
}
.mdc-button .mdc-button__label {
  position: relative;
}

.mat-mdc-button {
  padding: 0 var(--mat-button-text-horizontal-padding, 12px);
  height: var(--mat-button-text-container-height, 40px);
  font-family: var(--mat-button-text-label-text-font, var(--mat-sys-label-large-font));
  font-size: var(--mat-button-text-label-text-size, var(--mat-sys-label-large-size));
  letter-spacing: var(--mat-button-text-label-text-tracking, var(--mat-sys-label-large-tracking));
  text-transform: var(--mat-button-text-label-text-transform);
  font-weight: var(--mat-button-text-label-text-weight, var(--mat-sys-label-large-weight));
}
.mat-mdc-button, .mat-mdc-button .mdc-button__ripple {
  border-radius: var(--mat-button-text-container-shape, var(--mat-sys-corner-full));
}
.mat-mdc-button:not(:disabled) {
  color: var(--mat-button-text-label-text-color, var(--mat-sys-primary));
}
.mat-mdc-button[disabled], .mat-mdc-button.mat-mdc-button-disabled {
  cursor: default;
  pointer-events: none;
  color: var(--mat-button-text-disabled-label-text-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
}
.mat-mdc-button.mat-mdc-button-disabled-interactive {
  pointer-events: auto;
}
.mat-mdc-button:has(.material-icons, mat-icon, [matButtonIcon]) {
  padding: 0 var(--mat-button-text-with-icon-horizontal-padding, 16px);
}
.mat-mdc-button > .mat-icon {
  margin-right: var(--mat-button-text-icon-spacing, 8px);
  margin-left: var(--mat-button-text-icon-offset, -4px);
}
[dir=rtl] .mat-mdc-button > .mat-icon {
  margin-right: var(--mat-button-text-icon-offset, -4px);
  margin-left: var(--mat-button-text-icon-spacing, 8px);
}
.mat-mdc-button .mdc-button__label + .mat-icon {
  margin-right: var(--mat-button-text-icon-offset, -4px);
  margin-left: var(--mat-button-text-icon-spacing, 8px);
}
[dir=rtl] .mat-mdc-button .mdc-button__label + .mat-icon {
  margin-right: var(--mat-button-text-icon-spacing, 8px);
  margin-left: var(--mat-button-text-icon-offset, -4px);
}
.mat-mdc-button .mat-ripple-element {
  background-color: var(--mat-button-text-ripple-color, color-mix(in srgb, var(--mat-sys-primary) calc(var(--mat-sys-pressed-state-layer-opacity) * 100%), transparent));
}
.mat-mdc-button .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-button-text-state-layer-color, var(--mat-sys-primary));
}
.mat-mdc-button.mat-mdc-button-disabled .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-button-text-disabled-state-layer-color, var(--mat-sys-on-surface-variant));
}
.mat-mdc-button:hover > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-text-hover-state-layer-opacity, var(--mat-sys-hover-state-layer-opacity));
}
.mat-mdc-button.cdk-program-focused > .mat-mdc-button-persistent-ripple::before, .mat-mdc-button.cdk-keyboard-focused > .mat-mdc-button-persistent-ripple::before, .mat-mdc-button.mat-mdc-button-disabled-interactive:focus > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-text-focus-state-layer-opacity, var(--mat-sys-focus-state-layer-opacity));
}
.mat-mdc-button:active > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-text-pressed-state-layer-opacity, var(--mat-sys-pressed-state-layer-opacity));
}
.mat-mdc-button .mat-mdc-button-touch-target {
  position: absolute;
  top: 50%;
  height: var(--mat-button-text-touch-target-size, 48px);
  display: var(--mat-button-text-touch-target-display, block);
  left: 0;
  right: 0;
  transform: translateY(-50%);
}

.mat-mdc-unelevated-button {
  transition: box-shadow 280ms cubic-bezier(0.4, 0, 0.2, 1);
  height: var(--mat-button-filled-container-height, 40px);
  font-family: var(--mat-button-filled-label-text-font, var(--mat-sys-label-large-font));
  font-size: var(--mat-button-filled-label-text-size, var(--mat-sys-label-large-size));
  letter-spacing: var(--mat-button-filled-label-text-tracking, var(--mat-sys-label-large-tracking));
  text-transform: var(--mat-button-filled-label-text-transform);
  font-weight: var(--mat-button-filled-label-text-weight, var(--mat-sys-label-large-weight));
  padding: 0 var(--mat-button-filled-horizontal-padding, 24px);
}
.mat-mdc-unelevated-button > .mat-icon {
  margin-right: var(--mat-button-filled-icon-spacing, 8px);
  margin-left: var(--mat-button-filled-icon-offset, -8px);
}
[dir=rtl] .mat-mdc-unelevated-button > .mat-icon {
  margin-right: var(--mat-button-filled-icon-offset, -8px);
  margin-left: var(--mat-button-filled-icon-spacing, 8px);
}
.mat-mdc-unelevated-button .mdc-button__label + .mat-icon {
  margin-right: var(--mat-button-filled-icon-offset, -8px);
  margin-left: var(--mat-button-filled-icon-spacing, 8px);
}
[dir=rtl] .mat-mdc-unelevated-button .mdc-button__label + .mat-icon {
  margin-right: var(--mat-button-filled-icon-spacing, 8px);
  margin-left: var(--mat-button-filled-icon-offset, -8px);
}
.mat-mdc-unelevated-button .mat-ripple-element {
  background-color: var(--mat-button-filled-ripple-color, color-mix(in srgb, var(--mat-sys-on-primary) calc(var(--mat-sys-pressed-state-layer-opacity) * 100%), transparent));
}
.mat-mdc-unelevated-button .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-button-filled-state-layer-color, var(--mat-sys-on-primary));
}
.mat-mdc-unelevated-button.mat-mdc-button-disabled .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-button-filled-disabled-state-layer-color, var(--mat-sys-on-surface-variant));
}
.mat-mdc-unelevated-button:hover > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-filled-hover-state-layer-opacity, var(--mat-sys-hover-state-layer-opacity));
}
.mat-mdc-unelevated-button.cdk-program-focused > .mat-mdc-button-persistent-ripple::before, .mat-mdc-unelevated-button.cdk-keyboard-focused > .mat-mdc-button-persistent-ripple::before, .mat-mdc-unelevated-button.mat-mdc-button-disabled-interactive:focus > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-filled-focus-state-layer-opacity, var(--mat-sys-focus-state-layer-opacity));
}
.mat-mdc-unelevated-button:active > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-filled-pressed-state-layer-opacity, var(--mat-sys-pressed-state-layer-opacity));
}
.mat-mdc-unelevated-button .mat-mdc-button-touch-target {
  position: absolute;
  top: 50%;
  height: var(--mat-button-filled-touch-target-size, 48px);
  display: var(--mat-button-filled-touch-target-display, block);
  left: 0;
  right: 0;
  transform: translateY(-50%);
}
.mat-mdc-unelevated-button:not(:disabled) {
  color: var(--mat-button-filled-label-text-color, var(--mat-sys-on-primary));
  background-color: var(--mat-button-filled-container-color, var(--mat-sys-primary));
}
.mat-mdc-unelevated-button, .mat-mdc-unelevated-button .mdc-button__ripple {
  border-radius: var(--mat-button-filled-container-shape, var(--mat-sys-corner-full));
}
.mat-mdc-unelevated-button[disabled], .mat-mdc-unelevated-button.mat-mdc-button-disabled {
  cursor: default;
  pointer-events: none;
  color: var(--mat-button-filled-disabled-label-text-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
  background-color: var(--mat-button-filled-disabled-container-color, color-mix(in srgb, var(--mat-sys-on-surface) 12%, transparent));
}
.mat-mdc-unelevated-button.mat-mdc-button-disabled-interactive {
  pointer-events: auto;
}

.mat-mdc-raised-button {
  transition: box-shadow 280ms cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: var(--mat-button-protected-container-elevation-shadow, var(--mat-sys-level1));
  height: var(--mat-button-protected-container-height, 40px);
  font-family: var(--mat-button-protected-label-text-font, var(--mat-sys-label-large-font));
  font-size: var(--mat-button-protected-label-text-size, var(--mat-sys-label-large-size));
  letter-spacing: var(--mat-button-protected-label-text-tracking, var(--mat-sys-label-large-tracking));
  text-transform: var(--mat-button-protected-label-text-transform);
  font-weight: var(--mat-button-protected-label-text-weight, var(--mat-sys-label-large-weight));
  padding: 0 var(--mat-button-protected-horizontal-padding, 24px);
}
.mat-mdc-raised-button > .mat-icon {
  margin-right: var(--mat-button-protected-icon-spacing, 8px);
  margin-left: var(--mat-button-protected-icon-offset, -8px);
}
[dir=rtl] .mat-mdc-raised-button > .mat-icon {
  margin-right: var(--mat-button-protected-icon-offset, -8px);
  margin-left: var(--mat-button-protected-icon-spacing, 8px);
}
.mat-mdc-raised-button .mdc-button__label + .mat-icon {
  margin-right: var(--mat-button-protected-icon-offset, -8px);
  margin-left: var(--mat-button-protected-icon-spacing, 8px);
}
[dir=rtl] .mat-mdc-raised-button .mdc-button__label + .mat-icon {
  margin-right: var(--mat-button-protected-icon-spacing, 8px);
  margin-left: var(--mat-button-protected-icon-offset, -8px);
}
.mat-mdc-raised-button .mat-ripple-element {
  background-color: var(--mat-button-protected-ripple-color, color-mix(in srgb, var(--mat-sys-primary) calc(var(--mat-sys-pressed-state-layer-opacity) * 100%), transparent));
}
.mat-mdc-raised-button .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-button-protected-state-layer-color, var(--mat-sys-primary));
}
.mat-mdc-raised-button.mat-mdc-button-disabled .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-button-protected-disabled-state-layer-color, var(--mat-sys-on-surface-variant));
}
.mat-mdc-raised-button:hover > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-protected-hover-state-layer-opacity, var(--mat-sys-hover-state-layer-opacity));
}
.mat-mdc-raised-button.cdk-program-focused > .mat-mdc-button-persistent-ripple::before, .mat-mdc-raised-button.cdk-keyboard-focused > .mat-mdc-button-persistent-ripple::before, .mat-mdc-raised-button.mat-mdc-button-disabled-interactive:focus > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-protected-focus-state-layer-opacity, var(--mat-sys-focus-state-layer-opacity));
}
.mat-mdc-raised-button:active > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-protected-pressed-state-layer-opacity, var(--mat-sys-pressed-state-layer-opacity));
}
.mat-mdc-raised-button .mat-mdc-button-touch-target {
  position: absolute;
  top: 50%;
  height: var(--mat-button-protected-touch-target-size, 48px);
  display: var(--mat-button-protected-touch-target-display, block);
  left: 0;
  right: 0;
  transform: translateY(-50%);
}
.mat-mdc-raised-button:not(:disabled) {
  color: var(--mat-button-protected-label-text-color, var(--mat-sys-primary));
  background-color: var(--mat-button-protected-container-color, var(--mat-sys-surface));
}
.mat-mdc-raised-button, .mat-mdc-raised-button .mdc-button__ripple {
  border-radius: var(--mat-button-protected-container-shape, var(--mat-sys-corner-full));
}
@media (hover: hover) {
  .mat-mdc-raised-button:hover {
    box-shadow: var(--mat-button-protected-hover-container-elevation-shadow, var(--mat-sys-level2));
  }
}
.mat-mdc-raised-button:focus {
  box-shadow: var(--mat-button-protected-focus-container-elevation-shadow, var(--mat-sys-level1));
}
.mat-mdc-raised-button:active, .mat-mdc-raised-button:focus:active {
  box-shadow: var(--mat-button-protected-pressed-container-elevation-shadow, var(--mat-sys-level1));
}
.mat-mdc-raised-button[disabled], .mat-mdc-raised-button.mat-mdc-button-disabled {
  cursor: default;
  pointer-events: none;
  color: var(--mat-button-protected-disabled-label-text-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
  background-color: var(--mat-button-protected-disabled-container-color, color-mix(in srgb, var(--mat-sys-on-surface) 12%, transparent));
}
.mat-mdc-raised-button[disabled].mat-mdc-button-disabled, .mat-mdc-raised-button.mat-mdc-button-disabled.mat-mdc-button-disabled {
  box-shadow: var(--mat-button-protected-disabled-container-elevation-shadow, var(--mat-sys-level0));
}
.mat-mdc-raised-button.mat-mdc-button-disabled-interactive {
  pointer-events: auto;
}

.mat-mdc-outlined-button {
  border-style: solid;
  transition: border 280ms cubic-bezier(0.4, 0, 0.2, 1);
  height: var(--mat-button-outlined-container-height, 40px);
  font-family: var(--mat-button-outlined-label-text-font, var(--mat-sys-label-large-font));
  font-size: var(--mat-button-outlined-label-text-size, var(--mat-sys-label-large-size));
  letter-spacing: var(--mat-button-outlined-label-text-tracking, var(--mat-sys-label-large-tracking));
  text-transform: var(--mat-button-outlined-label-text-transform);
  font-weight: var(--mat-button-outlined-label-text-weight, var(--mat-sys-label-large-weight));
  border-radius: var(--mat-button-outlined-container-shape, var(--mat-sys-corner-full));
  border-width: var(--mat-button-outlined-outline-width, 1px);
  padding: 0 var(--mat-button-outlined-horizontal-padding, 24px);
}
.mat-mdc-outlined-button > .mat-icon {
  margin-right: var(--mat-button-outlined-icon-spacing, 8px);
  margin-left: var(--mat-button-outlined-icon-offset, -8px);
}
[dir=rtl] .mat-mdc-outlined-button > .mat-icon {
  margin-right: var(--mat-button-outlined-icon-offset, -8px);
  margin-left: var(--mat-button-outlined-icon-spacing, 8px);
}
.mat-mdc-outlined-button .mdc-button__label + .mat-icon {
  margin-right: var(--mat-button-outlined-icon-offset, -8px);
  margin-left: var(--mat-button-outlined-icon-spacing, 8px);
}
[dir=rtl] .mat-mdc-outlined-button .mdc-button__label + .mat-icon {
  margin-right: var(--mat-button-outlined-icon-spacing, 8px);
  margin-left: var(--mat-button-outlined-icon-offset, -8px);
}
.mat-mdc-outlined-button .mat-ripple-element {
  background-color: var(--mat-button-outlined-ripple-color, color-mix(in srgb, var(--mat-sys-primary) calc(var(--mat-sys-pressed-state-layer-opacity) * 100%), transparent));
}
.mat-mdc-outlined-button .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-button-outlined-state-layer-color, var(--mat-sys-primary));
}
.mat-mdc-outlined-button.mat-mdc-button-disabled .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-button-outlined-disabled-state-layer-color, var(--mat-sys-on-surface-variant));
}
.mat-mdc-outlined-button:hover > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-outlined-hover-state-layer-opacity, var(--mat-sys-hover-state-layer-opacity));
}
.mat-mdc-outlined-button.cdk-program-focused > .mat-mdc-button-persistent-ripple::before, .mat-mdc-outlined-button.cdk-keyboard-focused > .mat-mdc-button-persistent-ripple::before, .mat-mdc-outlined-button.mat-mdc-button-disabled-interactive:focus > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-outlined-focus-state-layer-opacity, var(--mat-sys-focus-state-layer-opacity));
}
.mat-mdc-outlined-button:active > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-outlined-pressed-state-layer-opacity, var(--mat-sys-pressed-state-layer-opacity));
}
.mat-mdc-outlined-button .mat-mdc-button-touch-target {
  position: absolute;
  top: 50%;
  height: var(--mat-button-outlined-touch-target-size, 48px);
  display: var(--mat-button-outlined-touch-target-display, block);
  left: 0;
  right: 0;
  transform: translateY(-50%);
}
.mat-mdc-outlined-button:not(:disabled) {
  color: var(--mat-button-outlined-label-text-color, var(--mat-sys-primary));
  border-color: var(--mat-button-outlined-outline-color, var(--mat-sys-outline));
}
.mat-mdc-outlined-button[disabled], .mat-mdc-outlined-button.mat-mdc-button-disabled {
  cursor: default;
  pointer-events: none;
  color: var(--mat-button-outlined-disabled-label-text-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
  border-color: var(--mat-button-outlined-disabled-outline-color, color-mix(in srgb, var(--mat-sys-on-surface) 12%, transparent));
}
.mat-mdc-outlined-button.mat-mdc-button-disabled-interactive {
  pointer-events: auto;
}

.mat-tonal-button {
  transition: box-shadow 280ms cubic-bezier(0.4, 0, 0.2, 1);
  height: var(--mat-button-tonal-container-height, 40px);
  font-family: var(--mat-button-tonal-label-text-font, var(--mat-sys-label-large-font));
  font-size: var(--mat-button-tonal-label-text-size, var(--mat-sys-label-large-size));
  letter-spacing: var(--mat-button-tonal-label-text-tracking, var(--mat-sys-label-large-tracking));
  text-transform: var(--mat-button-tonal-label-text-transform);
  font-weight: var(--mat-button-tonal-label-text-weight, var(--mat-sys-label-large-weight));
  padding: 0 var(--mat-button-tonal-horizontal-padding, 24px);
}
.mat-tonal-button:not(:disabled) {
  color: var(--mat-button-tonal-label-text-color, var(--mat-sys-on-secondary-container));
  background-color: var(--mat-button-tonal-container-color, var(--mat-sys-secondary-container));
}
.mat-tonal-button, .mat-tonal-button .mdc-button__ripple {
  border-radius: var(--mat-button-tonal-container-shape, var(--mat-sys-corner-full));
}
.mat-tonal-button[disabled], .mat-tonal-button.mat-mdc-button-disabled {
  cursor: default;
  pointer-events: none;
  color: var(--mat-button-tonal-disabled-label-text-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
  background-color: var(--mat-button-tonal-disabled-container-color, color-mix(in srgb, var(--mat-sys-on-surface) 12%, transparent));
}
.mat-tonal-button.mat-mdc-button-disabled-interactive {
  pointer-events: auto;
}
.mat-tonal-button > .mat-icon {
  margin-right: var(--mat-button-tonal-icon-spacing, 8px);
  margin-left: var(--mat-button-tonal-icon-offset, -8px);
}
[dir=rtl] .mat-tonal-button > .mat-icon {
  margin-right: var(--mat-button-tonal-icon-offset, -8px);
  margin-left: var(--mat-button-tonal-icon-spacing, 8px);
}
.mat-tonal-button .mdc-button__label + .mat-icon {
  margin-right: var(--mat-button-tonal-icon-offset, -8px);
  margin-left: var(--mat-button-tonal-icon-spacing, 8px);
}
[dir=rtl] .mat-tonal-button .mdc-button__label + .mat-icon {
  margin-right: var(--mat-button-tonal-icon-spacing, 8px);
  margin-left: var(--mat-button-tonal-icon-offset, -8px);
}
.mat-tonal-button .mat-ripple-element {
  background-color: var(--mat-button-tonal-ripple-color, color-mix(in srgb, var(--mat-sys-on-secondary-container) calc(var(--mat-sys-pressed-state-layer-opacity) * 100%), transparent));
}
.mat-tonal-button .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-button-tonal-state-layer-color, var(--mat-sys-on-secondary-container));
}
.mat-tonal-button.mat-mdc-button-disabled .mat-mdc-button-persistent-ripple::before {
  background-color: var(--mat-button-tonal-disabled-state-layer-color, var(--mat-sys-on-surface-variant));
}
.mat-tonal-button:hover > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-tonal-hover-state-layer-opacity, var(--mat-sys-hover-state-layer-opacity));
}
.mat-tonal-button.cdk-program-focused > .mat-mdc-button-persistent-ripple::before, .mat-tonal-button.cdk-keyboard-focused > .mat-mdc-button-persistent-ripple::before, .mat-tonal-button.mat-mdc-button-disabled-interactive:focus > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-tonal-focus-state-layer-opacity, var(--mat-sys-focus-state-layer-opacity));
}
.mat-tonal-button:active > .mat-mdc-button-persistent-ripple::before {
  opacity: var(--mat-button-tonal-pressed-state-layer-opacity, var(--mat-sys-pressed-state-layer-opacity));
}
.mat-tonal-button .mat-mdc-button-touch-target {
  position: absolute;
  top: 50%;
  height: var(--mat-button-tonal-touch-target-size, 48px);
  display: var(--mat-button-tonal-touch-target-display, block);
  left: 0;
  right: 0;
  transform: translateY(-50%);
}

.mat-mdc-button,
.mat-mdc-unelevated-button,
.mat-mdc-raised-button,
.mat-mdc-outlined-button,
.mat-tonal-button {
  -webkit-tap-highlight-color: transparent;
}
.mat-mdc-button .mat-mdc-button-ripple,
.mat-mdc-button .mat-mdc-button-persistent-ripple,
.mat-mdc-button .mat-mdc-button-persistent-ripple::before,
.mat-mdc-unelevated-button .mat-mdc-button-ripple,
.mat-mdc-unelevated-button .mat-mdc-button-persistent-ripple,
.mat-mdc-unelevated-button .mat-mdc-button-persistent-ripple::before,
.mat-mdc-raised-button .mat-mdc-button-ripple,
.mat-mdc-raised-button .mat-mdc-button-persistent-ripple,
.mat-mdc-raised-button .mat-mdc-button-persistent-ripple::before,
.mat-mdc-outlined-button .mat-mdc-button-ripple,
.mat-mdc-outlined-button .mat-mdc-button-persistent-ripple,
.mat-mdc-outlined-button .mat-mdc-button-persistent-ripple::before,
.mat-tonal-button .mat-mdc-button-ripple,
.mat-tonal-button .mat-mdc-button-persistent-ripple,
.mat-tonal-button .mat-mdc-button-persistent-ripple::before {
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  position: absolute;
  pointer-events: none;
  border-radius: inherit;
}
.mat-mdc-button .mat-mdc-button-ripple,
.mat-mdc-unelevated-button .mat-mdc-button-ripple,
.mat-mdc-raised-button .mat-mdc-button-ripple,
.mat-mdc-outlined-button .mat-mdc-button-ripple,
.mat-tonal-button .mat-mdc-button-ripple {
  overflow: hidden;
}
.mat-mdc-button .mat-mdc-button-persistent-ripple::before,
.mat-mdc-unelevated-button .mat-mdc-button-persistent-ripple::before,
.mat-mdc-raised-button .mat-mdc-button-persistent-ripple::before,
.mat-mdc-outlined-button .mat-mdc-button-persistent-ripple::before,
.mat-tonal-button .mat-mdc-button-persistent-ripple::before {
  content: "";
  opacity: 0;
}
.mat-mdc-button .mdc-button__label,
.mat-mdc-button .mat-icon,
.mat-mdc-unelevated-button .mdc-button__label,
.mat-mdc-unelevated-button .mat-icon,
.mat-mdc-raised-button .mdc-button__label,
.mat-mdc-raised-button .mat-icon,
.mat-mdc-outlined-button .mdc-button__label,
.mat-mdc-outlined-button .mat-icon,
.mat-tonal-button .mdc-button__label,
.mat-tonal-button .mat-icon {
  z-index: 1;
  position: relative;
}
.mat-mdc-button .mat-focus-indicator,
.mat-mdc-unelevated-button .mat-focus-indicator,
.mat-mdc-raised-button .mat-focus-indicator,
.mat-mdc-outlined-button .mat-focus-indicator,
.mat-tonal-button .mat-focus-indicator {
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  position: absolute;
  border-radius: inherit;
}
.mat-mdc-button:focus-visible > .mat-focus-indicator::before,
.mat-mdc-unelevated-button:focus-visible > .mat-focus-indicator::before,
.mat-mdc-raised-button:focus-visible > .mat-focus-indicator::before,
.mat-mdc-outlined-button:focus-visible > .mat-focus-indicator::before,
.mat-tonal-button:focus-visible > .mat-focus-indicator::before {
  content: "";
  border-radius: inherit;
}
.mat-mdc-button._mat-animation-noopable,
.mat-mdc-unelevated-button._mat-animation-noopable,
.mat-mdc-raised-button._mat-animation-noopable,
.mat-mdc-outlined-button._mat-animation-noopable,
.mat-tonal-button._mat-animation-noopable {
  transition: none !important;
  animation: none !important;
}
.mat-mdc-button > .mat-icon,
.mat-mdc-unelevated-button > .mat-icon,
.mat-mdc-raised-button > .mat-icon,
.mat-mdc-outlined-button > .mat-icon,
.mat-tonal-button > .mat-icon {
  display: inline-block;
  position: relative;
  vertical-align: top;
  font-size: 1.125rem;
  height: 1.125rem;
  width: 1.125rem;
}

.mat-mdc-outlined-button .mat-mdc-button-ripple,
.mat-mdc-outlined-button .mdc-button__ripple {
  top: -1px;
  left: -1px;
  bottom: -1px;
  right: -1px;
}

.mat-mdc-unelevated-button .mat-focus-indicator::before,
.mat-tonal-button .mat-focus-indicator::before,
.mat-mdc-raised-button .mat-focus-indicator::before {
  margin: calc(calc(var(--mat-focus-indicator-border-width, 3px) + 2px) * -1);
}

.mat-mdc-outlined-button .mat-focus-indicator::before {
  margin: calc(calc(var(--mat-focus-indicator-border-width, 3px) + 3px) * -1);
}
`,`@media (forced-colors: active) {
  .mat-mdc-button:not(.mdc-button--outlined),
  .mat-mdc-unelevated-button:not(.mdc-button--outlined),
  .mat-mdc-raised-button:not(.mdc-button--outlined),
  .mat-mdc-outlined-button:not(.mdc-button--outlined),
  .mat-mdc-button-base.mat-tonal-button,
  .mat-mdc-icon-button.mat-mdc-icon-button,
  .mat-mdc-outlined-button .mdc-button__ripple {
    outline: solid 1px;
  }
}
`],encapsulation:2,changeDetection:0})}return t})();function Im(t){return t.hasAttribute("mat-raised-button")?"elevated":t.hasAttribute("mat-stroked-button")?"outlined":t.hasAttribute("mat-flat-button")?"filled":t.hasAttribute("mat-button")?"text":null}var x0=(()=>{class t{static \u0275fac=function(n){return new(n||t)};static \u0275mod=L({type:t});static \u0275inj=P({imports:[Us,_n]})}return t})();export{re as a,jt as b,Vi as c,rc as d,ic as e,sc as f,vp as g,Kn as h,Oc as i,ie as j,pa as k,ou as l,su as m,Yg as n,xf as o,bb as p,vb as q,j as r,Lr as s,dn as t,Br as u,Ob as v,$f as w,Ve as x,yt as y,Dt as z,Hr as A,X as B,Hf as C,be as D,mn as E,Vf as F,ce as G,gn as H,bs as I,_s as J,ws as K,im as L,om as M,sm as N,cs as O,Gr as P,Yr as Q,Ur as R,um as S,lm as T,py as U,wt as V,Ny as W,Py as X,By as Y,Uy as Z,wy as _,Cy as $,Iy as aa,n0 as ba,Bs as ca,mm as da,_n as ea,Us as fa,Em as ga,M0 as ha,x0 as ia};
